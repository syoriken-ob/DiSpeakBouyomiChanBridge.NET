using net.boilingwater.Framework.Common;
using net.boilingwater.Framework.Common.Utils;

using NUnit.Framework;
//            
namespace net.boilingwater.Framework.CommonTest.Utils
{
    public class SerializeUtilTest
    {
        /// <summary>
        /// SerializeJson Test
        /// <para>
        /// case of:<br/>
        ///     is null<br/>
        ///     is empty string<br/>
        ///     is empty JSON<br/>
        ///     is not JSON parsable string
        ///     is JSON parsable string
        /// </para>
        /// </summary>
        /// <param name="json">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(SerializeJsonTestSource))]
        public string SerializeJsonTest(object? obj) => SerializeUtil.SerializeJson(obj);

        public static readonly TestCaseData[] SerializeJsonTestSource =
        {
            new TestCaseData(null).Returns("{}").SetCategory("JsonToMultiDicTestSource").SetName("null"),
            new TestCaseData("").Returns("\"\"").SetCategory("JsonToMultiDicTestSource").SetName("empty string"),
            new TestCaseData(new MultiDic()
            {
                {"TestString", "AAAA"},
                {"TestBool", false},
                {"TestNumber", 0m},
                {"TestDic", new MultiDic(){ {"InnerString", "BBBB"}, {"InnerBool", true} } },
                {"TestList", new MultiList() {true, false, 0, "hoge"} },
            }).Returns("{\"TestString\":\"AAAA\",\"TestBool\":false,\"TestNumber\":0,\"TestDic\":{\"InnerString\":\"BBBB\",\"InnerBool\":true},\"TestList\":[true,false,0,\"hoge\"]}")
              .SetCategory("JsonToMultiDicTestSource").SetName("MultiDic"),
        };

        /// <summary>
        /// JsonToMultiDic Test
        /// <para>
        /// case of:<br/>
        ///     is null<br/>
        ///     is empty string<br/>
        ///     is empty JSON<br/>
        ///     is not JSON parsable string
        ///     is JSON parsable string
        /// </para>
        /// </summary>
        /// <param name="json">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(JsonToMultiDicTestSource))]
        public MultiDic JsonToMultiDicTest(string json)
        {
            return SerializeUtil.JsonToMultiDic(json);
        }

        public static readonly TestCaseData[] JsonToMultiDicTestSource =
        {
            new TestCaseData(null).Returns(new MultiDic()).SetCategory("JsonToMultiDicTestSource").SetName("null"),
            new TestCaseData(string.Empty).Returns(new MultiDic()).SetCategory("JsonToMultiDicTestSource").SetName("empty string"),
            new TestCaseData("{}").Returns(new MultiDic()).SetCategory("JsonToMultiDicTestSource").SetName("empty JSON"),
            new TestCaseData("[]").Returns(new MultiDic()).SetCategory("JsonToMultiDicTestSource").SetName("empty Array"),
            new TestCaseData("ほげふがぴよ").Returns(new MultiDic()).SetCategory("JsonToMultiDicTestSource").SetName("not JSON parsable string"),
            new TestCaseData("{ \"TestString\": \"AAAA\", \"TestBool\": false, \"TestNumber\": 0, \"TestDic\": { \"InnerString\": \"BBBB\", \"InnerBool\": true }, \"TestList\":[true, false, 0, \"hoge\"] }").Returns(new MultiDic()
            {
                {"TestString", "AAAA"},
                {"TestBool", false},
                {"TestNumber", 0m},
                {"TestDic", new MultiDic(){ {"InnerString", "BBBB"}, {"InnerBool", true} } },
                {"TestList", new MultiList() {true, false, 0, "hoge"} },
            }).SetCategory("JsonToMultiDicTestSource").SetName("JSON parsable string"),
        };
    }
}
