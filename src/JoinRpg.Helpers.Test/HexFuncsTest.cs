using Shouldly;
using Xunit;

namespace JoinRpg.Helpers.Test;

public class HexFuncsTest
{
    [Fact]
    public void TestFromHexString()
    {
        var str = "010203040506070E";
        var result = str.FromHexString();
        result.ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6, 7, 0x0E });
    }
}
