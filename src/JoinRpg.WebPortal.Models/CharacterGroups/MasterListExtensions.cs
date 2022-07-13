using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Web.Models.CharacterGroups;

public static class MasterListExtensions
{
    public static IEnumerable<MasterViewModel> GetMasterListViewModel(
        this Project project)
    {
        return project.ProjectAcls
            .Select(acl => new MasterViewModel(acl.User.UserId, acl.User.ExtractDisplayName()))
            .OrderBy(a => a.DisplayName.DisplayName);
    }

    public static IEnumerable<SelectListItem> ToSelectListItems(this IEnumerable<MasterViewModel> views)
    {
        return views.Select(x => new SelectListItem { Value = x.MasterId.ToString(), Text = x.DisplayName.DisplayName });
    }

    [Obsolete("")]
    public static IEnumerable<ImprovedSelectListItem> ToImprovedSelectListItems(this IEnumerable<MasterViewModel> views)
    {
        return views.Select(x =>
            new ImprovedSelectListItem
            {
                Value = x.MasterId.ToString(),
                Text = x.DisplayName.DisplayName,
                ExtraSearch = x.DisplayName.FullName ?? "",
                Subtext = x.DisplayName.FullName ?? "",
            });
    }
}
