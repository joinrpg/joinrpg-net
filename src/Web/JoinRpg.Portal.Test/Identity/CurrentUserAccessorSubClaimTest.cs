using System.Security.Claims;
using Joinrpg.Web.Identity;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Test.Identity;

/// <summary>
/// Разбор пользователя из claim <c>sub</c> у OAuth-токенов (MCP, ADR012).
/// </summary>
/// <remarks>
/// IdPortal пишет в <c>sub</c> <c>ToString()</c> типизированного идентификатора, то есть
/// «UserId(3)». Разбор через <c>int.TryParse</c> на таком значении молча давал null, и
/// <c>ICurrentUserAccessor.UserIdentification</c> бросал «Authorization required here» — на dev
/// это выходило как 500 у каждого MCP-инструмента при полностью валидном токене.
///
/// Формат в IdPortal менять нельзя: то же значение лежит в поле <c>Subject</c> авторизации, по
/// которому ищется выданное согласие. Поэтому терпимым обязан быть разбор.
/// </remarks>
public class CurrentUserAccessorSubClaimTest
{
    [Theory]
    [InlineData("UserId(3)", "то, что реально пишет IdPortal")]
    [InlineData("3", "простое число — на случай, если формат когда-нибудь упростят")]
    public void ResolvesUserFromSubClaim(string subValue, string why)
    {
        var accessor = CreateAccessor(new Claim("sub", subValue));

        accessor.UserIdOrDefault.ShouldBe(3, why);
    }

    [Fact]
    public void UnparsableSub_GivesNoUser()
    {
        // Не «падает с исключением», а именно не опознаёт пользователя.
        var accessor = CreateAccessor(new Claim("sub", "не идентификатор"));

        accessor.UserIdOrDefault.ShouldBeNull();
    }

    [Fact]
    public void UidClaim_StillWins()
    {
        // У JWT-токенов x-api идентификатор лежит в uid, и этот путь трогать было нельзя.
        var accessor = CreateAccessor(new Claim("uid", "7"), new Claim("sub", "UserId(3)"));

        accessor.UserIdOrDefault.ShouldBe(7);
    }

    private static CurrentUserAccessor CreateAccessor(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        return new CurrentUserAccessor(new HttpContextAccessor { HttpContext = context });
    }
}
