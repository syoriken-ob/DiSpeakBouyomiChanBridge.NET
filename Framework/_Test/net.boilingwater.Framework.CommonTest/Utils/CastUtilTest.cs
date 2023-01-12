using System;

using net.boilingwater.Framework.Common.Utils;

using NUnit.Framework;

namespace net.boilingwater.Framework.CommonTest.Utils
{
    /// <summary>
    /// 型変換ユーティリティクラスのテストクラス
    /// </summary>
    public class CastUtilTest
    {
        /// <summary>
        /// ToDecimal Test
        /// <para>case of:
        ///     is decimal
        ///     is int number
        ///     is string plus number
        ///     is string minus number
        ///     is failed cast
        ///     is 1000 separator
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToDecimalTestSource))]
        public decimal ToDecimalTest(object obj)
        {
            return CastUtil.ToDecimal(obj);
        }

        public static readonly TestCaseData[] ToDecimalTestSource =
        {
            new TestCaseData(1.1m).Returns(1.1m).SetName("Decimal"),
            new TestCaseData(1).Returns(1m).SetName("Decimal parse int"),
            new TestCaseData("1.1").Returns(1.1m).SetName("Decimal parse plus"),
            new TestCaseData("-1.1").Returns(-1.1m).SetName("Decimal parse minus"),
            new TestCaseData("a").Returns(default(decimal)).SetName("Decimal default"),
            new TestCaseData("100,000").Returns(100000m).SetName("Decimal 1000 separator"),
        };

        /// <summary>
        /// ToInteger Test
        /// case of:
        ///     is int
        ///     is string plus number
        ///     is string minus number
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToIntegerTestSource))]
        public int ToIntegerTest(object obj)
        {
            return CastUtil.ToInteger(obj);
        }

        public static readonly TestCaseData[] ToIntegerTestSource =
        {
            new TestCaseData(1).Returns(1).SetName("Int"),
            new TestCaseData("1.1").Returns(1).SetName("Int ToDecimal plus"),
            new TestCaseData("-1.1").Returns(-1).SetName("Int ToDecimal minus"),
        };

        /// <summary>
        /// ToUnsignedInteger Test
        /// case of:
        ///     is uint
        ///     is string plus number
        ///     is string minus number
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToUnsignedIntegerTestSource))]
        public uint ToUnsignedIntegerTest(object obj)
        {
            return CastUtil.ToUnsignedInteger(obj);
        }

        public static readonly TestCaseData[] ToUnsignedIntegerTestSource =
        {
            new TestCaseData((uint)1).Returns(1).SetName("UInt"),
            new TestCaseData("1.1").Returns(1).SetName("UInt ToDecimal plus"),
            new TestCaseData("-1.1").Returns(default(uint)).SetName("UInt ToDecimal minus"),
        };

        /// <summary>
        /// ToLong Test
        /// case of:
        ///     is long
        ///     is string plus number
        ///     is string minus number
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToLongTestSource))]
        public long ToLongTest(object obj)
        {
            return CastUtil.ToLong(obj);
        }

        public static readonly TestCaseData[] ToLongTestSource =
        {
            new TestCaseData(1L).Returns(1L).SetName("Long"),
            new TestCaseData("1.1").Returns(1L).SetName("Long ToDecimal plus"),
            new TestCaseData("-1.1").Returns(-1L).SetName("Long ToDecimal minus"),
        };

        /// <summary>
        /// ToUnsignedLong Test
        /// case of:
        ///     is ulong
        ///     is string plus number
        ///     is string minus number
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToUnsignedLongTestSource))]
        public ulong ToUnsignedLongTest(object obj)
        {
            return CastUtil.ToUnsignedLong(obj);
        }

        public static readonly TestCaseData[] ToUnsignedLongTestSource =
        {
            new TestCaseData((ulong)1L).Returns(1L).SetName("ULong"),
            new TestCaseData("1.1").Returns(1L).SetName("ULong ToDecimal plus"),
            new TestCaseData("-1.1").Returns(default(ulong)).SetName("ULong ToDecimal minus"),
        };

        /// <summary>
        /// ToDouble Test
        /// case of:
        ///     is double
        ///     is string plus number
        ///     is string minus number
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToDoubleTestSource))]
        public double ToDoubleTest(object obj)
        {
            return CastUtil.ToDouble(obj);
        }

        public static readonly TestCaseData[] ToDoubleTestSource =
        {
            new TestCaseData(1.1).Returns(1.1).SetName("Double"),
            new TestCaseData("1.1").Returns(1.1).SetName("Double ToDecimal plus"),
            new TestCaseData("-1.1").Returns(-1.1).SetName("Double ToDecimal minus"),
        };

        /// <summary>
        /// ToBoolean Test
        /// case of:
        ///     is bool
        ///     is string true
        ///     is string false
        ///     is failed cast
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToBooleanTestSource))]
        public bool ToBooleanTest(object obj)
        {
            return CastUtil.ToBoolean(obj);
        }

        public static readonly TestCaseData[] ToBooleanTestSource =
        {
            new TestCaseData(true).Returns(true).SetName("Bool"),
            new TestCaseData("true").Returns(true).SetName("Bool parse true"),
            new TestCaseData("false").Returns(false).SetName("Bool parse false"),
            new TestCaseData(1).Returns(default(bool)).SetName("Bool default"),
        };

        /// <summary>
        /// ToString Test
        /// case of:
        ///     is null
        ///     is string
        ///     is not ref obj(num)
        ///     is enum
        ///     is other ref obj
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToStringTestSource))]
        public string ToStringTest(object obj)
        {
            return CastUtil.ToString(obj);
        }

        public static readonly TestCaseData[] ToStringTestSource =
        {
            new TestCaseData(null).Returns(string.Empty).SetName("String null"),
            new TestCaseData("sample").Returns("sample").SetName("String cast"),
            new TestCaseData(1).Returns("1").SetName("String num"),
            new TestCaseData(ToStringTestEnum.HOGE).Returns("HOGE").SetName("String enum"),
        };

        // TODO: Write ToObjectTest
        // テストケース考えるのが面倒くさい

        public enum ToStringTestEnum
        { HOGE }

        /// <summary>
        /// ToGuid Test
        /// </summary>
        /// <param name="obj">cast target</param>
        /// <returns>cast result</returns>
        [TestCaseSource(nameof(ToGuidTestSource))]
        public Guid ToGuidTest(object obj) => CastUtil.ToGuid(obj);

        public static readonly TestCaseData[] ToGuidTestSource =
        {
            new TestCaseData(null).Returns(Guid.Empty).SetCategory("ToGuidTestSource").SetName("Guid null"),
            new TestCaseData("d1a6eadf-c842-40b9-9aa3-a2d87a6aef86").Returns(Guid.Parse("d1a6eadf-c842-40b9-9aa3-a2d87a6aef86")).SetCategory("ToGuidTestSource").SetName("Guid Castable string"),
            new TestCaseData("d2d1c1bd-6805-11ed-bd01-bb9bcd5fc526").Returns(Guid.Parse("d2d1c1bd-6805-11ed-bd01-bb9bcd5fc526")).SetCategory("ToGuidTestSource").SetName("Guid Castable string v1 uuid"),
            new TestCaseData("{d1a6eadf-c842-40b9-9aa3-a2d87a6aef86}").Returns(Guid.Parse("d1a6eadf-c842-40b9-9aa3-a2d87a6aef86")).SetCategory("ToGuidTestSource").SetName("Guid Castable string with Bracket"),
            new TestCaseData("hoge").Returns(Guid.Empty).SetCategory("ToGuidTestSource").SetName("Guid not Castable string"),
            new TestCaseData(Guid.Parse("d1a6eadf-c842-40b9-9aa3-a2d87a6aef86")).Returns(Guid.Parse("d1a6eadf-c842-40b9-9aa3-a2d87a6aef86")).SetCategory("ToGuidTestSource").SetName("Guid guid"),
        };

        /// <summary>
        /// ToDateTime Test
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [TestCaseSource(nameof(ToDateTimeTestSource))]
        public DateTime ToDateTimeTest(object obj) => CastUtil.ToDateTime(obj);

        public static readonly TestCaseData[] ToDateTimeTestSource =
        {
            new TestCaseData(null).Returns(DateTime.MinValue).SetCategory("ToDateTimeTestSource").SetName("DateTime null"),
            new TestCaseData("").Returns(DateTime.MinValue).SetCategory("ToDateTimeTestSource").SetName("DateTime empty string"),
            new TestCaseData("2022-11-19 22:43:45").Returns(new DateTime(2022, 11, 19, 22, 43, 45,0)).SetCategory("ToDateTimeTestSource").SetName("DateTime Castable string"),
            new TestCaseData("2022-11-19T22:43:45").Returns(new DateTime(2022, 11, 19, 22, 43, 45,0)).SetCategory("ToDateTimeTestSource").SetName("DateTime Castable string T"),
            new TestCaseData("2022-11-19T22:43:45.001").Returns(new DateTime(2022, 11, 19, 22, 43, 45,1)).SetCategory("ToDateTimeTestSource").SetName("DateTime Castable string T with Milliseconds"),
        };
    }
}
