using Joinrpg.Web.Identity;
using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.Portal.Test;
public class PermissionEncoderTests
{
    [Fact]
    public void EncodeSimple()
    {
        PermissionEncoder.Encode([Permission.None, Permission.CanManagePlots]).ShouldBe("0,8");
    }

    [Fact]
    public void DecodeSimple()
    {
        PermissionEncoder.Decode("1,2 ").ShouldBe([Permission.CanChangeFields, Permission.CanChangeProjectProperties], ignoreOrder: true);
    }

    [Theory]
    [InlineData("1, 2", Permission.CanChangeProjectProperties)]
    [InlineData("2, 1", Permission.CanChangeProjectProperties)]
    public void ShouldBePresent(string encoded, Permission permission)
    {
        PermissionEncoder.HasPermission(encoded, permission).ShouldBeTrue();
    }

    [Theory]
    [InlineData("1, 2", Permission.CanManageClaims)]
    [InlineData("2, 1", Permission.CanManagePlots)]
    public void ShouldBeNotPresent(string encoded, Permission permission)
    {
        PermissionEncoder.HasPermission(encoded, permission).ShouldBeFalse();
    }
}
