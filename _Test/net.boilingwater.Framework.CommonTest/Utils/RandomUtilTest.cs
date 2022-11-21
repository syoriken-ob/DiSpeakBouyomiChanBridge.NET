using net.boilingwater.Framework.Common.Utils;

using NUnit.Framework;

namespace net.boilingwater.Framework.CommonTest.Utils
{
    public class RandomUtilTest
    {
        /// <summary>
        /// CreateRandomNumber Test
        /// </summary>
        /// <param name="digits">桁数</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(CreateRandomNumberTestSource))]
        public int CreateRandomNumberTest(int digits) => RandomUtil.CreateRandomNumber(digits);

        public static readonly TestCaseData[] CreateRandomNumberTestSource =
        {
            new TestCaseData(-1).Returns(0).SetCategory("CreateRandomNumberTest").SetName("-1"),
            new TestCaseData(0).Returns(0).SetCategory("CreateRandomNumberTest").SetName("0")
        };

        [Test]
        public void Test_9桁の乱数を生成する() => Assert.AreEqual(9, RandomUtil.CreateRandomNumber(9).ToString().Length);
    }
}
