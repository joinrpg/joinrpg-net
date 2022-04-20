using System.Globalization;
using Shouldly;

namespace JoinRpg.TestHelpers
{
    /// <summary>
    /// Class that allows to compare two enums
    /// </summary>
    public static class EnumerationTestComparer
    {
        /// <summary>
        /// Compare two enums
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        public static void EnsureSame<TFirst, TSecond>()
            where TFirst : struct, Enum
            where TSecond : struct, Enum
        {
            var firstMax = GetEnumMaxValue<TFirst>();
            var secondMax = GetEnumMaxValue<TSecond>();
            firstMax.ShouldBe(secondMax, "should be of same length");

            foreach (var first in Enum.GetValues<TFirst>())
            {
                var otherEnum = EnumFromInt<TSecond>(Convert.ToInt32(first, CultureInfo.InvariantCulture));
                var firstString = first.ToString();
                var otherString = otherEnum.ToString();
                firstString.ShouldBe(otherString);
            }
        }

        private static TEnum EnumFromInt<TEnum>(int i) where TEnum : struct, Enum => (TEnum)Enum.ToObject(typeof(TEnum), i);

        private static int GetEnumMaxValue<TEnum>() where TEnum : struct, Enum
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            return Convert.ToInt32(Enum.GetValues<TEnum>().Max(), CultureInfo.InvariantCulture);
        }
    }
}
