using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JoinRpg.Web.Helpers;

public static class ModelStateExtensions
{
    public static void AddErrors(this ModelStateDictionary modelStateDictionary, IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            modelStateDictionary.AddModelError("", error.Description);
        }
    }
}
