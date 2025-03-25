using Shouldly;
using Xunit;

namespace JoinRpg.Helpers.Test;

public class CountHelperTest
{
    [Theory]

    [InlineData(0, "0 персонажей")]
    [InlineData(1, "1 персонаж")]
    [InlineData(2, "2 персонажа")]
    [InlineData(5, "5 персонажей")]
    [InlineData(11, "11 персонажей")]
    [InlineData(21, "21 персонаж")]
    [InlineData(22, "22 персонажа")]
    [InlineData(25, "25 персонажей")]
    [InlineData(101, "101 персонаж")]
    [InlineData(102, "102 персонажа")]
    [InlineData(111, "111 персонажей")]
    [InlineData(121, "121 персонаж")]
    [InlineData(122, "122 персонажа")]
    [InlineData(-1, "-1 персонаж")]
    [InlineData(-2, "-2 персонажа")]
    [InlineData(-5, "-5 персонажей")]
    [InlineData(-11, "-11 персонажей")]
    [InlineData(-21, "-21 персонаж")]
    [InlineData(-22, "-22 персонажа")]
    [InlineData(-25, "-25 персонажей")]
    public void TestCount(int count, string expected)
    {
        CountHelper.DisplayCount(count, "персонаж", "персонажа", "персонажей").ShouldBe(expected);
    }
}
