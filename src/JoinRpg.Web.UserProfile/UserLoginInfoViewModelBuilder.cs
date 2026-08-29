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
                    ProviderKey = link.Id?.ToString(),
                    NeedToReLink = false,
                    ProviderLink = link.Link,
                };
            }
            else
            {
                return new UserLoginInfoViewModel()
                {
                    AllowLink = link is null,
                    AllowUnlink = false,
                    IsOnlyLoginMethod = false,
                    LoginProvider = provider,
                    ProviderKey = null,
                    NeedToReLink = link is not null,
                    ProviderLink = link?.Link,
                };
            }
        }
    }
}
