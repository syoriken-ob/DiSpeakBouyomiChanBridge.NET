using System.Security.Cryptography;

namespace net.boilingwater.Framework.Common.Utils
{
    /// <summary>
    /// 乱数のユーティリティクラス
    /// </summary>
    public static class RandomUtil
    {
        /// <summary>
        /// <paramref name="digits"/>で指定された桁数の乱数を生成します。
        /// </summary>
        /// <param name="digits">生成する乱数の桁数</param>
        /// <returns></returns>
        public static int CreateRandomNumber(int digits)
        {
            if (digits <= 0)
            {
                return 0;
            }

            return RandomNumberGenerator.GetInt32(
                                            CastUtil.ToInteger('1' + new string('0', digits - 1)),
                                            CastUtil.ToInteger(new string('9', digits)) + 1
                                         );
        }
    }
}
