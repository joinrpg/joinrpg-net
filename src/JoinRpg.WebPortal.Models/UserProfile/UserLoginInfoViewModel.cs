using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
#nullable enable

    public record UserLoginInfoViewModel
    {
        public record ProviderDescViewModel(string ProviderId, string FriendlyName)
        {
            public static readonly ProviderDescViewModel Google = new("Google", "Google");
            public static readonly ProviderDescViewModel Vk = new("Vkontakte", "ВК");
        }

        public ProviderDescViewModel LoginProvider { get; init; } = null!;

        public string? ProviderLink { get; set; }

        public string? ProviderKey { get; set; }

        public bool AllowLink { get; set; }
        public bool AllowUnlink { get; set; }
        public bool NeedToReLink { get; set; }
    }

    public static class UserLoginInfoViewModelBuilder
    {
        public static IEnumerable<UserLoginInfoViewModel> GetSocialLogins(this User user)
        {
            var canRemoveLogins = user.PasswordHash != null || user.ExternalLogins.Count > 1;
            yield return GetModel(UserLoginInfoViewModel.ProviderDescViewModel.Google);
            var vk = GetModel(UserLoginInfoViewModel.ProviderDescViewModel.Vk);
            if (vk.ProviderKey is not null)
            {
                vk.ProviderLink = $"https://vk.com/id{vk.ProviderKey}";
            }

            if (vk.ProviderKey is null && user.Extra?.Vk is not null)
            {
                vk.NeedToReLink = true;
                vk.AllowLink = false;
                vk.ProviderLink = $"https://vk.com/id{user.Extra?.Vk}";
            }

            yield return vk;

            UserLoginInfoViewModel GetModel(UserLoginInfoViewModel.ProviderDescViewModel provider)
            {
                if (user.ExternalLogins.SingleOrDefault(l =>
                    l.Provider.ToLowerInvariant() == provider.ProviderId.ToLowerInvariant()
                    ) is UserExternalLogin login)
                {
                    return new UserLoginInfoViewModel()
                    {
                        AllowLink = false,
                        AllowUnlink = canRemoveLogins,
                        LoginProvider = provider,
                        ProviderKey = login.Key,
                        NeedToReLink = false,
                        ProviderLink = null,
                    };
                }
                else
                {
                    return new UserLoginInfoViewModel()
                    {
                        AllowLink = true,
                        AllowUnlink = false,
                        LoginProvider = provider,
                        ProviderKey = null,
                        NeedToReLink = false,
                        ProviderLink = null,
                    };
                }
            }
        }
    }
}
