using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authentication;

namespace JoinRpg.Web.Helpers
{
    public static class AuthModelBuilers
    {
        public static AuthenticationDescriptionViewModel ToViewModel(this AuthenticationScheme ol)
        {
            return new AuthenticationDescriptionViewModel()
            {
                AuthenticationType = ol.Name,
                Caption = ol.DisplayName
            };
        }
    }
}
