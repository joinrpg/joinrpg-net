using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Users;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using SystemClaim = System.Security.Claims.Claim;

namespace JoinRpg.IdPortal.OAuthServer.Test;

public class GetUserInfoMethodTest
{
    private static readonly UserIdentification UserId1 = new UserIdentification(1);

    private static UserInfo BuildUserInfo(
        string email = "test@example.com",
        bool emailConfirmed = true,
        string? bornName = "Иван",
        string? surName = "Иванов",
        string? fatherName = null,
        string? phoneNumber = null,
        AvatarIdentification? selectedAvatarId = null)
    {
        return new UserInfo(
            UserId: UserId1,
            Social: new UserSocialNetworks(null, null, null, null, ContactsAccessType.OnlyForMasters),
            ActiveClaims: [],
            ActiveProjects: [],
            AllProjects: [],
            IsAdmin: false,
            SelectedAvatarId: selectedAvatarId,
            Email: new Email(email),
            EmailConfirmed: emailConfirmed,
            UserFullName: new UserFullName(null, BornName.FromOptional(bornName), SurName.FromOptional(surName), FatherName.FromOptional(fatherName)),
            VerifiedProfileFlag: false,
            PhoneNumber: phoneNumber);
    }

    private static IOptions<JoinRpgHostNamesOptions> DefaultOptions => Options.Create(new JoinRpgHostNamesOptions
    {
        IdHost = "id.test",
        MainHost = "main.test",
        KogdaIgraHost = "kogdaigra.test",
        RatingHost = "rating.test",
    });

    private class FakeUserRepository(UserInfo userInfo) : IUserRepository
    {
        Task<UserInfo?> IUserRepository.GetUserInfo(UserIdentification userId)
            => Task.FromResult<UserInfo?>(userInfo);

        Task<User> IUserRepository.GetById(int id) => throw new NotImplementedException();
        Task<User> IUserRepository.WithProfile(int userId) => throw new NotImplementedException();
        Task<User> IUserRepository.GetWithSubscribe(int currentUserId) => throw new NotImplementedException();
        Task<UserAvatar> IUserRepository.LoadAvatar(AvatarIdentification userAvatarId) => throw new NotImplementedException();
        Task<IReadOnlyCollection<UserInfo>> IUserRepository.GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
        Task<IReadOnlyCollection<UserInfoHeader>> IUserRepository.GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
        Task<IReadOnlyCollection<UserInfoHeader>> IUserRepository.GetAdminUserInfoHeaders() => throw new NotImplementedException();
    }

    private class FakeAvatarLoader(string uri) : IAvatarLoader
    {
        private readonly Uri _uri = new Uri(uri);

        Task<AvatarInfo> IAvatarLoader.GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
            => Task.FromResult(new AvatarInfo(_uri, recommendedSize));
    }

    private static ClaimsPrincipal BuildPrincipal(string? subClaim, params string[] scopes)
    {
        var identity = new ClaimsIdentity();
        if (subClaim is not null)
        {
            identity.AddClaim(new SystemClaim(OpenIddictConstants.Claims.Subject, subClaim));
        }
        var principal = new ClaimsPrincipal(identity);
        if (scopes.Length > 0)
        {
            principal.SetScopes(scopes);
        }
        return principal;
    }

    private static async Task<Dictionary<string, object?>> GetOkResult(
        ClaimsPrincipal principal,
        UserInfo userInfo,
        IOptions<JoinRpgHostNamesOptions>? options = null,
        string avatarUrl = "https://example.com/avatar.png")
    {
        var result = await CallUserInfo(principal, userInfo, avatarUrl, options);
        var ok = result.Result.ShouldBeOfType<Ok<Dictionary<string, object?>>>();
        return ok.Value!;
    }

    private static async Task<Results<ForbidHttpResult, Ok<Dictionary<string, object?>>>> CallUserInfo(
        ClaimsPrincipal principal,
        UserInfo userInfo,
        string avatarUrl = "https://example.com/avatar.png",
        IOptions<JoinRpgHostNamesOptions>? options = null)
    {
        return await OAuthServerRegistration.GetUserInfoMethod(
                    principal,
                    new FakeUserRepository(userInfo),
                    options ?? DefaultOptions,
                    new FakeAvatarLoader(avatarUrl));
    }

    [Fact]
    public async Task WhenNoSubjectClaim_ReturnsForbid()
    {
        var principal = BuildPrincipal(null);
        var result = await CallUserInfo(principal, BuildUserInfo());
        result.Result.ShouldBeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task WhenSubjectClaimInvalid_ReturnsForbid()
    {
        var principal = BuildPrincipal("invalid");
        var result = await CallUserInfo(principal, BuildUserInfo());
        result.Result.ShouldBeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task WithValidToken_ReturnsSubClaim()
    {
        var dict = await GetOkResult(BuildPrincipal(UserId1.ToString()), BuildUserInfo());
        dict.ShouldContainKey(OpenIddictConstants.Claims.Subject);
    }

    [Fact]
    public async Task WithEmailScope_ReturnsEmail()
    {
        var dict = await GetOkResult(
            BuildPrincipal(UserId1.ToString(), OpenIddictConstants.Scopes.Email),
            BuildUserInfo(email: "test@example.com", emailConfirmed: true));
        dict.ShouldContainKey(OpenIddictConstants.Claims.Email);
        dict[OpenIddictConstants.Claims.Email].ShouldBe("test@example.com");
        dict[OpenIddictConstants.Claims.EmailVerified].ShouldBe(true);
    }

    [Fact]
    public async Task WithEmailScope_Unconfirmed_EmailVerifiedFalse()
    {
        var dict = await GetOkResult(
            BuildPrincipal(UserId1.ToString(), OpenIddictConstants.Scopes.Email),
            BuildUserInfo(emailConfirmed: false));
        dict[OpenIddictConstants.Claims.EmailVerified].ShouldBe(false);
    }

    [Fact]
    public async Task WithoutEmailScope_NoEmailInResponse()
    {
        var dict = await GetOkResult(BuildPrincipal(UserId1.ToString()), BuildUserInfo());
        dict.ShouldNotContainKey(OpenIddictConstants.Claims.Email);
    }

    [Fact]
    public async Task WithProfileScope_ReturnsNameClaims()
    {
        var dict = await GetOkResult(
            BuildPrincipal(UserId1.ToString(), OpenIddictConstants.Scopes.Profile),
            BuildUserInfo(bornName: "Иван", surName: "Иванов", fatherName: "Иваныч"));
        dict.ShouldContainKey(OpenIddictConstants.Claims.GivenName);
        dict.ShouldContainKey(OpenIddictConstants.Claims.FamilyName);
        dict.ShouldContainKey(OpenIddictConstants.Claims.Name);
        dict[OpenIddictConstants.Claims.GivenName].ShouldBe("Иван");
        dict[OpenIddictConstants.Claims.FamilyName].ShouldBe("Иванов");
        dict[OpenIddictConstants.Claims.MiddleName].ShouldBe("Иваныч");
    }

    [Fact]
    public async Task WithPhoneScope_ReturnsPhoneNumber()
    {
        var dict = await GetOkResult(
            BuildPrincipal(UserId1.ToString(), OpenIddictConstants.Scopes.Phone),
            BuildUserInfo(phoneNumber: "+79001234567"));
        dict.ShouldContainKey(OpenIddictConstants.Claims.PhoneNumber);
        dict[OpenIddictConstants.Claims.PhoneNumber].ShouldBe("+79001234567");
    }

    [Fact]
    public async Task WithAvatarAndProfileScope_ReturnsPicture()
    {
        var avatarId = new AvatarIdentification(42);
        var avatarUrl = "https://cdn.example.com/avatar.png";
        var dict = await GetOkResult(
            BuildPrincipal(UserId1.ToString(), OpenIddictConstants.Scopes.Profile),
            BuildUserInfo(selectedAvatarId: avatarId),
            avatarUrl: avatarUrl);
        dict.ShouldContainKey(OpenIddictConstants.Claims.Picture);
        dict[OpenIddictConstants.Claims.Picture].ShouldBe(new Uri(avatarUrl));
    }
}
