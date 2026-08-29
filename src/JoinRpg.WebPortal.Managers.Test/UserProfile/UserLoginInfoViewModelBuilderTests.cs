using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.WebPortal.Managers.Test.UserProfile;

public class UserLoginInfoViewModelBuilderTests
{
    private static UserInfo BuildUserInfo(
        VkSocialLink? vk = null,
        TelegramSocialLink? telegram = null,
        bool hasPassword = true,
        bool hasSingleLoginMethod = false)
    {
        return new UserInfo(
            UserId: new UserIdentification(1),
            Social: new UserSocialNetworks(telegram, null, null, vk, ContactsAccessType.Public),
            ActiveClaims: [],
            ActiveProjects: [],
            AllProjects: [],
            IsAdmin: false,
            SelectedAvatarId: null,
            Email: new Email("player@example.com"),
            EmailConfirmed: true,
            UserFullName: new UserFullName(new PrefferedName("Player"), null, null, null),
            VerifiedProfileFlag: false,
            PhoneNumber: null,
            HasPassword: hasPassword,
            HasSingleLoginMethod: hasSingleLoginMethod);
    }

    [Fact]
    public void GetSocialLogins_NoProfileNoLogin_AllowsLink()
    {
        var user = BuildUserInfo();

        var vk = user.GetSocialLogins().Single(x => x.LoginProvider == ProviderDescViewModel.Vk);

        vk.AllowLink.ShouldBeTrue();
        vk.AllowUnlink.ShouldBeFalse();
        vk.NeedToReLink.ShouldBeFalse();
        vk.ProviderKey.ShouldBeNull();
    }

    [Fact]
    public void GetSocialLogins_LinkedWithOtherLoginMethod_AllowsUnlinkAndNotOnlyMethod()
    {
        var user = BuildUserInfo(vk: new VkSocialLink(123, isVerified: true), hasPassword: true, hasSingleLoginMethod: false);

        var vk = user.GetSocialLogins().Single(x => x.LoginProvider == ProviderDescViewModel.Vk);

        vk.AllowLink.ShouldBeFalse();
        vk.AllowUnlink.ShouldBeTrue();
        vk.NeedToReLink.ShouldBeFalse();
        vk.IsOnlyLoginMethod.ShouldBeFalse();
        vk.ProviderKey.ShouldBe("123");
    }

    [Fact]
    public void GetSocialLogins_LinkedWithNoOtherLoginMethod_IsOnlyLoginMethod()
    {
        var user = BuildUserInfo(vk: new VkSocialLink(123, isVerified: true), hasPassword: false, hasSingleLoginMethod: true);

        var vk = user.GetSocialLogins().Single(x => x.LoginProvider == ProviderDescViewModel.Vk);

        vk.IsOnlyLoginMethod.ShouldBeTrue();
    }

    [Fact]
    public void GetSocialLogins_ProfileValuePresentButNotVerified_NeedsRelink()
    {
        var user = BuildUserInfo(vk: new VkSocialLink(123, isVerified: false));

        var vk = user.GetSocialLogins().Single(x => x.LoginProvider == ProviderDescViewModel.Vk);

        vk.AllowLink.ShouldBeFalse();
        vk.AllowUnlink.ShouldBeFalse();
        vk.NeedToReLink.ShouldBeTrue();
    }

    [Fact]
    public void GetSocialLogins_TelegramNeverBlocksUnlinkEvenAsSingleLoginMethod()
    {
        // Vk — единственный настоящий способ входа (CanLogin=true), Telegram привязан
        // дополнительно, но никогда не даёт войти сам по себе — отвязать его можно всегда.
        var user = BuildUserInfo(
            vk: new VkSocialLink(123, isVerified: true),
            telegram: new TelegramSocialLink(new TelegramChatId(456), isVerified: true),
            hasPassword: false,
            hasSingleLoginMethod: true);

        var telegram = user.GetSocialLogins().Single(x => x.LoginProvider == ProviderDescViewModel.Telegram);

        telegram.AllowUnlink.ShouldBeTrue();
        telegram.IsOnlyLoginMethod.ShouldBeFalse();
    }

    [Fact]
    public void GetSocialLogins_TelegramOnlyConnection_DoesNotCountAsLoginMethod()
    {
        // Telegram привязан, пароля и ВК нет — но т.к. Telegram не умеет логинить,
        // "единственного способа входа" фактически тоже нет.
        var user = BuildUserInfo(
            telegram: new TelegramSocialLink(new TelegramChatId(456), isVerified: true),
            hasPassword: false,
            hasSingleLoginMethod: false);

        var telegram = user.GetSocialLogins().Single(x => x.LoginProvider == ProviderDescViewModel.Telegram);

        telegram.AllowUnlink.ShouldBeTrue();
        telegram.IsOnlyLoginMethod.ShouldBeFalse();
    }
}
