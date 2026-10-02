using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/roles/{characterGroupId}/[action]")]
public class GameGroupsController(
    ICharacterGroupService characterGroupService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    ICharacterGroupRepository charGroupRepository
    ) : JoinControllerGameBase
{
    [HttpGet("~/{projectId}/roles/{characterGroupId?}")]
    [AllowAnonymous]
    public async Task<ActionResult> Index(ProjectIdentification projectId, int? characterGroupId)
    {

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var explicitGroupId = CharacterGroupIdentification.FromOptional(projectId, characterGroupId);

        // На /{projectId}/roles (без группы в URL) показываем сетку ролей по умолчанию — ту же,
        // что и /{projectId}/roleslist/{id}, но прямо здесь, без редиректа: /roles остаётся
        // каноническим адресом сетки ролей проекта. Классическая (транзиентная) сетка остаётся
        // только для проектов, у которых сеток ролей нет вообще, и для /roles/{characterGroupId}.
        if (explicitGroupId is null && projectInfo.DefaultRolesListId is { } defaultRolesListId)
        {
            return View(
              new GameRolesViewModel
              {
                  ProjectName = projectInfo.ProjectName,
                  ProjectId = projectId,
                  RolesListId = defaultRolesListId,
              });
        }

        var characterGroupId2 = explicitGroupId ?? projectInfo.GroupTree.RootGroupId;
        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(characterGroupId2);
        if (charGroupFullInfo is null)
        {
            return NotFound();
        }

        // Данные сетки теперь строит остров ProjectRoleGrid (классический режим) из транзиентной
        // настройки; здесь загружаем группу только для обрамления (Details-заголовок) и заголовка.
        return View(
          new GameRolesViewModel
          {
              ProjectName = projectInfo.ProjectName,
              ShowEditControls = projectInfo.HasEditRolesAccess(currentUserAccessor.UserIdentificationOrDefault),
              ProjectId = projectId,
              GridGroupId = explicitGroupId,
              RootGroupName = charGroupFullInfo.Name,
              Details = new CharacterGroupDetailsViewModel(charGroupFullInfo, projectInfo, currentUserAccessor.UserIdentificationOrDefault, GroupNavigationPage.Roles),
          });
    }

    [HttpGet("~/{projectId}/roles/hot")]
    [AllowAnonymous]
    public Task<ActionResult> Hot(int projectId) => Hot(projectId, null);

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> Hot(int projectId, int? characterGroupId)
    {
        var projectIdentification = new ProjectIdentification(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectIdentification);
        var explicitGroupId = CharacterGroupIdentification.FromOptional(projectIdentification, characterGroupId);
        if (explicitGroupId is { } groupId && await charGroupRepository.GetCharacterGroupFullInfo(groupId) is null)
        {
            return NotFound();
        }

        // Данные сетки строит остров ProjectRoleGrid (классический режим, HotRolesOnly) — здесь
        // только заголовок страницы, без обрамления группы (сетка не привязана к одной группе).
        return View(
          new GameRolesViewModel
          {
              ProjectName = projectInfo.ProjectName,
              ShowEditControls = projectInfo.HasEditRolesAccess(currentUserAccessor.UserIdentificationOrDefault),
              ProjectId = projectIdentification,
              GridGroupId = explicitGroupId,
              RootGroupName = "Горячие роли",
          });
    }

    [HttpGet, Authorize]
    [MasterAuthorize(Permission.CanEditRoles)]
    [ProjectShouldBeActive]
    public async Task<ActionResult> Edit(int projectId, int characterGroupId)
    {
        CharacterGroupIdentification charGroupId = new(new(projectId), characterGroupId);
        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(charGroupId);

        if (charGroupFullInfo is null)
        {
            return NotFound();
        }

        if (charGroupFullInfo.IsSpecial)
        {
            return Content("Can't edit special group");
        }

        if (charGroupFullInfo.IsRoot)
        {
            return RedirectToActionPermanent("Index");
        }

        return View(await BuildEditViewModel(charGroupFullInfo, charGroupId));
    }


    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanEditRoles), ProjectShouldBeActive]
    public async Task<ActionResult> Edit(EditCharacterGroupViewModel viewModel)
    {
        ProjectIdentification projectId = new(viewModel.ProjectId);
        CharacterGroupIdentification charGroupId = new(projectId, viewModel.CharacterGroupId);

        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(charGroupId);
        if (charGroupFullInfo is null)
        {
            return NotFound();
        }

        if (charGroupFullInfo.IsRoot)
        {
            return RedirectToActionPermanent("Index");
        }

        if (charGroupFullInfo.IsSpecial)
        {
            return Content("Can't edit special group");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEditViewModel(viewModel, charGroupFullInfo, charGroupId));
        }

        try
        {
            await characterGroupService.EditCharacterGroup(charGroupId,
                viewModel.Name, viewModel.IsPublic,
                [.. viewModel.ParentCharacterGroupIds],
                viewModel.Description);

            return RedirectToIndex(viewModel.ProjectId, viewModel.CharacterGroupId, "Details");
        }
        catch (Exception e)
        {
            AddModelException(e);
            return View(await BuildEditViewModel(viewModel, charGroupFullInfo, charGroupId));
        }
    }

    [HttpGet, MasterAuthorize(Permission.CanEditRoles), ProjectShouldBeActive]
    public async Task<ActionResult> Delete(ProjectIdentification projectId, int characterGroupId)
    {
        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(new(projectId, characterGroupId));
        if (charGroupFullInfo is null)
        {
            return NotFound();
        }

        return View(new DeleteCharacterGroupViewModel(charGroupFullInfo));
    }


    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken, ProjectShouldBeActive]
    public async Task<ActionResult> Delete(ProjectIdentification projectId, int characterGroupId, IFormCollection collection)
    {
        CharacterGroupIdentification charGroupId = new(projectId, characterGroupId);
        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(charGroupId);
        if (charGroupFullInfo is null)
        {
            return NotFound();
        }

        try
        {
            await characterGroupService.DeleteCharacterGroup(charGroupId);
            return RedirectToAction("Index", "GameGroups", new { projectId = projectId.Value, area = "" });
        }
        catch
        {
            return View(new DeleteCharacterGroupViewModel(charGroupFullInfo));
        }
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanEditRoles)]
    [ProjectShouldBeActive]
    public async Task<ActionResult> AddGroup(ProjectIdentification projectid, int charactergroupid)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectid);
        if (!projectInfo.GroupTree.Contains(new(projectid, charactergroupid)))
        {
            return NotFound();
        }

        return View(new AddCharacterGroupViewModel
        {
            ParentCharacterGroupIds = [new(projectid, charactergroupid)],
            ProjectId = projectid.Value,
            ProjectName = projectInfo.ProjectName.Value,
        });
    }

    [HttpPost]
    [MasterAuthorize(Permission.CanEditRoles)]
    [ValidateAntiForgeryToken]
    [ProjectShouldBeActive]
    public async Task<ActionResult> AddGroup(AddCharacterGroupViewModel viewModel, int charactergroupid)
    {
        ProjectIdentification projectId = new(viewModel.ProjectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!projectInfo.GroupTree.Contains(new(projectId, charactergroupid)))
        {
            return NotFound();
        }

        viewModel.ProjectName = projectInfo.ProjectName.Value;
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            List<CharacterGroupIdentification> parentCharacterGroupIds = [.. viewModel.ParentCharacterGroupIds];
            await characterGroupService.AddCharacterGroup(
              projectId,
              viewModel.Name, viewModel.IsPublic,
              parentCharacterGroupIds, viewModel.Description);

            return RedirectToRoles(parentCharacterGroupIds.First());
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return View(viewModel);
        }
    }

    private ActionResult RedirectToRoles(CharacterGroupIdentification characterGroupId, string action = "Index") => RedirectToIndex(characterGroupId.ProjectId, characterGroupId.CharacterGroupId, action);


    private async Task<EditCharacterGroupViewModel> BuildEditViewModel(
        CharacterGroupFullInfo charGroupFullInfo,
        CharacterGroupIdentification charGroupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(charGroupId.ProjectId);
        return new EditCharacterGroupViewModel
        {
            CharacterGroupId = charGroupId.CharacterGroupId,
            ParentCharacterGroupIds = [.. charGroupFullInfo.DirectParentGroupIds],
            Description = charGroupFullInfo.Description?.Value ?? "",
            IsPublic = charGroupFullInfo.IsPublic,
            Name = charGroupFullInfo.Name,
            ProjectId = charGroupId.ProjectId,
            ProjectName = projectInfo.ProjectName.Value,
            Marks = charGroupFullInfo.Marks.ToViewModel(),
        };
    }

    private async Task<EditCharacterGroupViewModel> BuildEditViewModel(
        EditCharacterGroupViewModel viewModel,
        CharacterGroupFullInfo charGroupFullInfo,
        CharacterGroupIdentification charGroupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(charGroupId.ProjectId);
        viewModel.ProjectId = charGroupId.ProjectId;
        viewModel.ProjectName = projectInfo.ProjectName.Value;
        viewModel.Marks = charGroupFullInfo.Marks.ToViewModel();
        return viewModel;
    }

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult> Details(ProjectIdentification projectId, int characterGroupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(new CharacterGroupIdentification(projectId, characterGroupId));
        if (charGroupFullInfo is null)
        {
            return NotFound();
        }

        var viewModel = new CharacterGroupDetailsViewModel(charGroupFullInfo, projectInfo, currentUserAccessor.UserIdentificationOrDefault, GroupNavigationPage.Home);
        return View(viewModel);
    }

}
