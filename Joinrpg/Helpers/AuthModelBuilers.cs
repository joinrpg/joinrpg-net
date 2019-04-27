using JoinRpg.Web.Models;
using Microsoft.Owin.Security;

namespace JoinRpg.Web.Helpers
{
    public static class AuthModelBuilers
    {
        public static AuthenticationDescriptionViewModel ToViewModel(this AuthenticationDescription ol)
        {
            return new AuthenticationDescriptionViewModel()
            {
                AuthenticationType = ol.AuthenticationType,
                Caption = ol.Caption
            };
        }
    }
}
