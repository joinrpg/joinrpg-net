using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.WebPortal.Managers.Claims;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace JoinRpg.Portal.Menu;

public class ProjectMenuViewComponent(
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentProjectAccessor currentProjectAccessor,
    IClaimsRepository claimsRepository,
    ICaptainRulesRepository captainRulesRepository,
    ILogger<ProjectMenuViewComponent> logger
    ) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProjectAccessor.ProjectId);

            if (currentUserAccessor.UserIdentificationOrDefault is not UserIdentification userId)
            {
                return GenerateAnonMenu(projectInfo);
            }

            var acl = projectInfo.Masters.FirstOrDefault(a => a.UserId == userId);

            if (acl != null)
            {
                return GenerateMasterMenu(projectInfo, acl);
            }
            else
            {
                return await GeneratePlayerMenu(projectInfo, userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке меню проекта");
            return Content("");
        }
    }

    private ViewViewComponentResult GenerateAnonMenu(ProjectInfo projectInfo)
    {
        var menuModel = new PlayerMenuViewModel(projectInfo, currentUserAccessor, [], []);
        return View("PlayerMenu", menuModel);
    }

    private async Task<IViewComponentResult> GeneratePlayerMenu(ProjectInfo projectInfo, UserIdentification userId)
    {
        var claims = (await claimsRepository.GetClaimsHeadersForPlayer(projectInfo.ProjectId, ClaimStatusSpec.Active, userId)).ToClaimViewModels().ToList();
        var captainAccessRules = await captainRulesRepository.GetCaptainRules(projectInfo.ProjectId, userId);
        var menuModel = new PlayerMenuViewModel(projectInfo, currentUserAccessor, claims, captainAccessRules);
        return View("PlayerMenu", menuModel);
    }

    private ViewViewComponentResult GenerateMasterMenu(ProjectInfo projectInfo, ProjectMasterInfo acl)
    {
        var menuModel = new MasterMenuViewModel(projectInfo, currentUserAccessor, acl.Permissions);
        return View("MasterMenu", menuModel);
    }
}
