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
            public static readonly ProviderDescViewModel Google = new ProviderDescViewModel("Google", "Google");
            public static readonly ProviderDescViewModel Vk = new ProviderDescViewModel("Vkontakte", "ВК");
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
            if (user.ExternalLogins.SingleOrDefault(l => l.Provider == "Google") is UserExternalLogin googleLogin)
            {
                yield return new UserLoginInfoViewModel()
                {
                    AllowLink = false,
                    AllowUnlink = canRemoveLogins,
                    LoginProvider = UserLoginInfoViewModel.ProviderDescViewModel.Google,
                    ProviderKey = googleLogin.Key,
                    NeedToReLink = false,
                    ProviderLink = null,
                };
            }
            else
            {
                yield return new UserLoginInfoViewModel()
                {
                    AllowLink = true,
                    AllowUnlink = false,
                    LoginProvider = UserLoginInfoViewModel.ProviderDescViewModel.Google,
                    ProviderKey = null,
                    NeedToReLink = false,
                    ProviderLink = null,
                };
            }
            if (user.ExternalLogins.SingleOrDefault(l => l.Provider == "Vkontakte") is UserExternalLogin vkLogin)
            {
                yield return new UserLoginInfoViewModel()
                {
                    AllowLink = false,
                    AllowUnlink = canRemoveLogins,
                    LoginProvider = UserLoginInfoViewModel.ProviderDescViewModel.Vk,
                    ProviderKey = vkLogin.Key,
                    NeedToReLink = false,
                    ProviderLink = $"https://vk.com/id{vkLogin.Key}",
                };
            }
            else if (user.Extra?.Vk != null)
            {
                yield return new UserLoginInfoViewModel()
                {
                    AllowLink = false,
                    AllowUnlink = false,
                    LoginProvider = UserLoginInfoViewModel.ProviderDescViewModel.Vk,
                    ProviderKey = null,
                    NeedToReLink = true,
                    ProviderLink = $"https://vk.com/id{user.Extra?.Vk}",
                };
            }
            else
            {
                yield return new UserLoginInfoViewModel()
                {
                    AllowLink = true,
                    AllowUnlink = false,
                    LoginProvider = UserLoginInfoViewModel.ProviderDescViewModel.Vk,
                    ProviderKey = null,
                    NeedToReLink = false,
                    ProviderLink = null,
                };
            }
        }
    }
}
