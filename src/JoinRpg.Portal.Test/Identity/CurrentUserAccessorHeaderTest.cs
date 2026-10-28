using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Interfaces;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Test.Identity;

/// <summary>
/// Шапка текущего пользователя (имя и аватарка) из любого источника, ADR026 §1: cookie, JWT x-api,
/// access-токен MCP и имперсонация.
/// </summary>
public class CurrentUserAccessorHeaderTest
{
    private static readonly Claim[] HeaderClaims =
    [
        new(JoinClaimTypes.DisplayName, "Ник"),
        new(JoinClaimTypes.FullName, "Иван Иванов"),
        new(JoinClaimTypes.AvatarId, "42"),
    ];

    public static TheoryData<string, string, string> Sources => new()
    {
        { "cookie", ClaimTypes.NameIdentifier, "3" },
        { "JWT x-api", "uid", "3" },
        { "MCP-токен", "sub", "UserId(3)" },
    };

    [Theory]
    [MemberData(nameof(Sources))]
    public void HeaderFromClaims(string source, string idClaimType, string idValue)
    {
        var accessor = CreateAccessor([new Claim(idClaimType, idValue), .. HeaderClaims]);

        accessor.UserIdOrDefault.ShouldBe(3, source);
        accessor.DisplayName.ShouldBe(new UserDisplayName("Ник", "Иван Иванов"), source);
        accessor.Avatar.ShouldBe(new AvatarIdentification(42), source);
        ((ICurrentUserAccessor)accessor).ToUserInfoHeader().ShouldBe(
            new UserInfoHeader(new UserIdentification(3), new UserDisplayName("Ник", "Иван Иванов")), source);
    }

    [Fact]
    public void NoAvatarClaim_GivesNullAvatar()
    {
        var accessor = CreateAccessor([
            new Claim("sub", "UserId(3)"),
            new Claim(JoinClaimTypes.DisplayName, "Ник"),
        ]);

        accessor.Avatar.ShouldBeNull();
        accessor.DisplayName.ShouldBe(new UserDisplayName("Ник", null));
    }

    [Fact]
    public void Impersonation_ReturnsGivenHeader_AndRestoresOnStop()
    {
        var accessor = CreateAccessor([new Claim(ClaimTypes.NameIdentifier, "3"), .. HeaderClaims]);
        var impersonate = (IImpersonateAccessor)accessor;

        impersonate.StartImpersonate(
            new UserIdentification(5), new UserDisplayName("Робот", null), new AvatarIdentification(7), IsAdmin: true);

        accessor.UserIdOrDefault.ShouldBe(5);
        accessor.DisplayName.ShouldBe(new UserDisplayName("Робот", null));
        accessor.Avatar.ShouldBe(new AvatarIdentification(7));
        accessor.IsAdmin.ShouldBeTrue();

        impersonate.StopImpersonate();

        accessor.UserIdOrDefault.ShouldBe(3);
        accessor.Avatar.ShouldBe(new AvatarIdentification(42));
    }

    [Fact]
    public void ImpersonationWithoutAvatar_GivesNullAvatar()
    {
        var accessor = CreateAccessor([new Claim(ClaimTypes.NameIdentifier, "3"), .. HeaderClaims]);

        ((IImpersonateAccessor)accessor).StartImpersonate(
            new UserIdentification(5), new UserDisplayName("Пользователь 5", null), avatar: null, IsAdmin: false);

        accessor.Avatar.ShouldBeNull();
    }

    private static CurrentUserAccessor CreateAccessor(Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        return new CurrentUserAccessor(new HttpContextAccessor { HttpContext = context });
    }
}
