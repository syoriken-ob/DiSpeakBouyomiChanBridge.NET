using System.Text.Encodings.Web;
using System.Text.Json;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace net.boilingwater.Framework.Common.Utils
{
    /// <summary>
    /// シリアライズ・デシリアライズ用のユーティリティクラス
    /// </summary>
    public static class SerializeUtil
    {
        /// <summary>
        /// オブジェクトをJSON文字列にシリアライズします。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializeJson(object? obj) => obj == null ? "{}" : Utf8Json.JsonSerializer.ToJsonString(obj);

        /// <summary>
        /// JSON文字列を<see cref="MultiDic"/>型にデシリアライズして取得します。
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns></returns>
        public static MultiDic JsonToMultiDic(string json)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
            {
                return new MultiDic();
            }

            try
            {
                var rawDic = JsonSerializer.Deserialize<MultiDic>(json, CreateJsonSerializerOption());
                return rawDic != null ? ConvertJsonElementToMultiDic(rawDic) : new MultiDic();
            }
            catch
            {
                return new MultiDic();
            }
        }

        /// <summary>
        /// JSON文字列を<see cref="MultiList"/>型にデシリアライズして取得します。
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns></returns>
        public static MultiList JsonToMultiList(string json)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
            {
                return new MultiList();
            }

            try
            {
                var rawList = JsonSerializer.Deserialize<MultiList>(json, CreateJsonSerializerOption());
                return rawList != null ? ConvertJsonElementToMultiDicFromMultiList(rawList) : new MultiList();
            }
            catch
            {
                return new MultiList();
            }
        }

        /// <summary>
        /// Yaml文字列を<typeparamref name="T"/>型にデシリアライズします。
        /// </summary>
        /// <typeparam name="T">デシリアライズしたい型</typeparam>
        /// <param name="yaml">Yaml文字列</param>
        /// <param name="isParameterNameCaseSensitive">デシリアライズ時、プロパティの大文字小文字を無視します。</param>
        /// <returns></returns>
        public static T DeserializeYaml<T>(string yaml, bool isParameterNameCaseSensitive = false)
        {
            var d = new DeserializerBuilder();
            if (!isParameterNameCaseSensitive)
            {
                d = d.WithNamingConvention(CamelCaseNamingConvention.Instance);
            }
            return d.Build().Deserialize<T>(yaml);
        }

        #region private

        private static JsonSerializerOptions CreateJsonSerializerOption()
        {
            return new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
        }

        private static MultiDic ConvertJsonElementToMultiDic(MultiDic rawDic)
        {
            foreach (var key in rawDic.Keys)
            {
                if (rawDic[key] is not JsonElement @element)
                {
                    continue;
                }
                rawDic[key] = Parse(element);
            }

            return rawDic;
        }

        private static MultiList ConvertJsonElementToMultiDicFromMultiList(MultiList rawList)
        {
            for (var i = 0; i < rawList.Count; i++)
            {
                if (rawList[i] is not JsonElement @element)
                {
                    continue;
                }
                rawList[i] = Parse(element);
            }

            return rawList;
        }

        private static object? Parse(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Undefined:
                    return element.GetRawText();

                case JsonValueKind.Object:
                    return ParseObjectToMultiDic(element);

                case JsonValueKind.Array:
                    return ParseList(element);

                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;

                case JsonValueKind.Number:
                    return element.GetDecimal();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return "";
            }
        }

        private static MultiDic ParseObjectToMultiDic(JsonElement element)
        {
            var dic = new MultiDic();

            foreach (var item in element.EnumerateObject())
            {
                dic.Add(item.Name, Parse(item.Value));
            }

            return dic;
        }

        private static MultiList ParseList(JsonElement element)
        {
            var list = new MultiList();

            foreach (var item in element.EnumerateArray())
            {
                list.Add(Parse(item));
            }

            return list;
        }

        #endregion private
    }
}
