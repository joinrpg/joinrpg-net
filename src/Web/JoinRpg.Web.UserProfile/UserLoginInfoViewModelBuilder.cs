using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Web.UserProfile;

public static class UserLoginInfoViewModelBuilder
{
    public static IEnumerable<UserLoginInfoViewModel> GetSocialLogins(this UserInfo user)
    {
        yield return GetModel(ProviderDescViewModel.Vk, user.Social.Vk);

        yield return GetModel(ProviderDescViewModel.Telegram, user.Social.Telegram);

        UserLoginInfoViewModel GetModel(ProviderDescViewModel provider, SocialLink? link)
        {
            if (link is { IsVerified: true })
            {
                return new UserLoginInfoViewModel()
                {
                    AllowLink = false,
                    AllowUnlink = true,
                    IsOnlyLoginMethod = user.HasSingleLoginMethod && link.CanLogin,
                    LoginProvider = provider,
                    NeedToReLink = false,
                    ProviderLink = link.Link,
                };
            }
            else
            {
                // Непровереннную привязку тоже можно удалить. Есть ли за ней настоящий
                // ExternalLogin, по данным профиля не видно (legacy-ВК хранит числовой id и
                // выглядит как привязанный), поэтому решает ExternalLoginProfileExtractor.RemoveLogin:
                // он смотрит реальные логины и в любом случае чистит legacy-поле профиля.
                return new UserLoginInfoViewModel()
                {
                    AllowLink = link is null,
                    AllowUnlink = link is not null,
                    IsOnlyLoginMethod = false,
                    LoginProvider = provider,
                    NeedToReLink = link is not null,
                    ProviderLink = link?.Link,
                };
            }
        }
    }
}
