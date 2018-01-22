using Shouldly;
using System;
using System.Globalization;
using System.Linq;

namespace JoinRpg.TestHelpers
{

  public static class EnumerationTestHelper
  {
    /// <summary>
    /// Compare two enums
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    public static void CheckEnums<TFirst, TSecond>()
      where TFirst : struct, IConvertible
      where TSecond : struct, IConvertible
    {
      var firstMax = GetEnumMaxValue<TFirst>();
      var secondMax = GetEnumMaxValue<TSecond>();
        firstMax.ShouldBe(secondMax);

      foreach (var first in Enum.GetValues(typeof (TFirst)).Cast<TFirst>())
      {
        var otherEnum = EnumFromInt<TSecond>(first.ToInt32(CultureInfo.InvariantCulture));
        var firstString = first.ToString(CultureInfo.InvariantCulture);
        var otherString = otherEnum.ToString(CultureInfo.InvariantCulture);
          firstString.ShouldBe(otherString);
      }
    }

    private static TEnum EnumFromInt<TEnum>(int i) where TEnum : struct, IConvertible
    {
      return (TEnum) Enum.ToObject(typeof (TEnum), i);
    }

    private static int GetEnumMaxValue<TEnum>() where TEnum : struct, IConvertible
    {
      if (!typeof (TEnum).IsEnum)
      {
        throw new ArgumentException("T must be an enumerated type");
      }
      return Enum.GetValues(typeof (TEnum))
        .Cast<TEnum>()
        .Max().ToInt32(CultureInfo.InvariantCulture);
    }
  }
}
