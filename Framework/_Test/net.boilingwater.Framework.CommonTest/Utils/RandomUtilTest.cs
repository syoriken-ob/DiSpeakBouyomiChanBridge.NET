using System.Collections.Generic;
using System.Linq;

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

        [TestCase(1, -1)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(5, 5)]
        [TestCase(6, 6)]
        [TestCase(7, 7)]
        [TestCase(8, 8)]
        [TestCase(9, 9)]
        public void CreateRandomNumber_指定された桁数で乱数が生成されるべき(int expectedDigit, int inputDigit)
        {
            var rand = RandomUtil.CreateRandomNumber(inputDigit);
            TestContext.WriteLine($"Values:{rand}");
            Assert.AreEqual(expectedDigit, rand.ToString().Length);
        }

        [TestCase(0, 0, -1, 10)]
        [TestCase(0, 0, 0, 10)]
        [TestCase(1, 9, 1, 50)]
        [TestCase(10, 99, 2, 50)]
        [TestCase(100, 999, 3, 50)]
        [TestCase(1000, 9999, 4, 50)]
        [TestCase(10000, 99999, 5, 50)]
        [TestCase(100000, 999999, 6, 50)]
        [TestCase(1000000, 9999999, 7, 50)]
        [TestCase(10000000, 99999999, 8, 50)]
        [TestCase(100000000, 999999999, 9, 50)]
        public void CreateRandomNumber_指定された範囲で乱数が生成されるべき(int expectedMin, int expectedMax, int inputDigit, int testCount)
        {
            var nums = new List<int>(testCount);
            for (var i = 0; i < testCount; i++)
            {
                nums.Add(RandomUtil.CreateRandomNumber(inputDigit));
            }
            TestContext.WriteLine("Values");
            nums.ForEach(TestContext.WriteLine);
            Assert.That(nums.All(rand => expectedMin <= rand && rand <= expectedMax));
        }
    }
}
