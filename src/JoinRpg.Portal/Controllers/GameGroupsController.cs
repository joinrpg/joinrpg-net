using System.Text.Json;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/roles/{characterGroupId}/[action]")]
public class GameGroupsController(
    IProjectRepository projectRepository,
    ICharacterGroupService characterGroupService,
    IUriService uriService,
    IUriLocator<UserLinkViewModel> userLinkLocator,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    ICharacterGroupRepository charGroupRepository
    ) : JoinControllerGameBase
{
    [HttpGet("~/{projectId}/roles/{characterGroupId?}")]
    [AllowAnonymous]
    public async Task<ActionResult> Index(ProjectIdentification projectId, int? characterGroupId)
    {
        var field = await projectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

        if (field == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        return View(
          new GameRolesViewModel
          {
              ProjectId = field.Project.ProjectId,
              ProjectName = field.Project.ProjectName,
              ShowEditControls = field.HasEditRolesAccess(currentUserAccessor.UserIdOrDefault),
              HasMasterAccess = projectInfo.HasMasterAccess(currentUserAccessor),
              Data = CharacterGroupListViewModel.GetGroups(field, currentUserAccessor.UserIdOrDefault, projectInfo),
              Details = new CharacterGroupDetailsViewModel(field, currentUserAccessor.UserIdOrDefault, GroupNavigationPage.Roles),
          });
    }

    [MasterAuthorize]
    [HttpGet("~/{projectId}/roles/{characterGroupId:int}/report")]
    [HttpGet("~/{projectId}/roles/all/report")]
    public async Task<ActionResult> Report(int projectId, int? characterGroupId)
    {
        var field = await projectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

        if (field == null)
        {
            return NotFound();
        }

        return View(
          new GameRolesReportViewModel
          {
              ProjectId = field.Project.ProjectId,
              Data = CharacterGroupReportViewModel.GetGroups(field),
              Details = new CharacterGroupDetailsViewModel(field, currentUserAccessor.UserIdOrDefault, GroupNavigationPage.Report),
              CheckinModuleEnabled = field.Project.Details.EnableCheckInModule,
          });
    }

    [HttpGet("~/{projectId}/roles/hot")]
    [AllowAnonymous]
    public Task<ActionResult> Hot(int projectId) => Hot(projectId, null);

    [HttpGet]
    public async Task<ActionResult> Hot(int projectId, int? characterGroupId)
    {
        var field = await projectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
        if (field == null)
        {
            return NotFound();
        }

        return View(await GetHotCharacters(field));
    }

    [HttpGet]
    [HttpGet("~/{projectId}/roles/hotjson")]
    public async Task<ActionResult> HotJson(int projectId, int characterGroupId, int? maxCount = null)
    {
        var field = await projectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
        if (field == null)
        {
            return NotFound();
        }

        var hotRoles = (await GetHotCharacters(field)).Shuffle().Take(maxCount ?? int.MaxValue);

        return ReturnJson(hotRoles.Select(ConvertCharacterToJson));
    }

    private async Task<IEnumerable<CharacterViewModel>> GetHotCharacters(CharacterGroup field)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(field.GetId().ProjectId);
        return CharacterGroupListViewModel.GetGroups(field, currentUserAccessor.UserIdOrDefault, projectInfo)
          .SelectMany(
            g => g.PublicCharacters.Where(ch => ch.IsHot && ch.IsFirstCopy)).Distinct();
    }

    [HttpGet("~/{projectId}/roles/{characterGroupId}/indexjson")]
    [AllowAnonymous]
    public async Task<ActionResult> IndexJson(ProjectIdentification projectId, int characterGroupId)
    {
        var field = await projectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
        if (field == null)
        {
            return NotFound();

        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var hasMasterAccess = projectInfo.HasMasterAccess(currentUserAccessor);
        return ReturnJson(new
        {
            field.Project.ProjectId,
            field.Project.ProjectName,
            ShowEditControls = hasMasterAccess,
            Groups = CharacterGroupListViewModel.GetGroups(field, currentUserAccessor.UserIdOrDefault, projectInfo).Select(
                g =>
                  new
                  {
                      g.CharacterGroupId,
                      g.Name,
                      g.DeepLevel,
                      g.FirstCopy,
                      Description = g.Description?.ToHtmlString(),
                      Path = g.Path.Select(gr => gr.Name),
                      PathIds = g.Path.Select(gr => gr.CharacterGroupId),
                      Characters = g.PublicCharacters.Select(ConvertCharacterToJson),
                      CanAddDirectClaim = false,
                      DirectClaimsCount = (string?)null,
                      DirectClaimLink = (string?)null,
                  }),
        });
    }

    private JsonResult ReturnJson(object data)
    {
        Response.Headers.Append("Access-Control-Allow-Origin", "*");

        return Json(data, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = null, // Pascal case
        });
    }

    private object ConvertCharacterToJson(CharacterViewModel ch)
    {
        return new
        {
            ch.CharacterId, //TODO Remove
            CharacterLink = uriService.Get(ch),
            ch.IsAvailable,
            ch.IsFirstCopy,
            ch.CharacterName,
            Description = ch.Description?.ToHtmlString(),
            PlayerName = ch.PlayerLink?.DisplayName,
            PlayerId = ch.PlayerLink?.UserId,
            PlayerLink = (ch.PlayerLink is null || ch.PlayerLink.ViewMode == ViewMode.Hide) ? null : userLinkLocator.GetUri(ch.PlayerLink).AbsoluteUri,
            ch.ActiveClaimsCount,
            ClaimLink =
            ch.IsAvailable
              ? GetFullyQualifiedUri("AddForCharacter", "Claim", new { ch.ProjectId, ch.CharacterId })
              : null,
        };
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
                [.. viewModel.ParentCharacterGroupIdInts.Select(id => new CharacterGroupIdentification(projectId, id))],
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
    public async Task<ActionResult> Delete(int projectId, int characterGroupId)
    {
        var field = await projectRepository.GetGroupAsync(projectId, characterGroupId);

        if (field == null)
        {
            return NotFound();
        }

        return View(field);
    }


    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken, ProjectShouldBeActive]
    public async Task<ActionResult> Delete(int projectId, int characterGroupId, IFormCollection collection)
    {
        var field = await projectRepository.GetGroupAsync(projectId, characterGroupId);

        if (field == null)
        {
            return NotFound();
        }

        var project = field.Project;
        try
        {
            await characterGroupService.DeleteCharacterGroup(new(new(projectId), characterGroupId));

            return RedirectToIndex(project);
        }
        catch
        {
            return View(field);
        }
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanEditRoles)]
    [ProjectShouldBeActive]
    public async Task<ActionResult> AddGroup(int projectid, int charactergroupid)
    {
        var field = await projectRepository.GetGroupAsync(projectid, charactergroupid);

        if (field == null)
        {
            return NotFound();
        }

        return View(FillFromCharacterGroup(new AddCharacterGroupViewModel()
        {
            ParentCharacterGroupIdInts = [field.CharacterGroupId],
        }, field));
    }

    [HttpPost]
    [MasterAuthorize(Permission.CanEditRoles)]
    [ValidateAntiForgeryToken]
    [ProjectShouldBeActive]
    public async Task<ActionResult> AddGroup(AddCharacterGroupViewModel viewModel, int charactergroupid)
    {
        var field = await projectRepository.GetGroupAsync(viewModel.ProjectId, charactergroupid);
        if (field == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(FillFromCharacterGroup(viewModel, field));
        }

        try
        {
            List<CharacterGroupIdentification> parentCharacterGroupIds = [.. CharacterGroupIdentification.FromList(viewModel.ParentCharacterGroupIdInts, new(viewModel.ProjectId))];
            await characterGroupService.AddCharacterGroup(
              new(viewModel.ProjectId),
              viewModel.Name, viewModel.IsPublic,
              parentCharacterGroupIds, viewModel.Description);

            return RedirectToRoles(parentCharacterGroupIds.First());
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return View(FillFromCharacterGroup(viewModel, field));
        }
    }

    private ActionResult RedirectToRoles(CharacterGroupIdentification characterGroupId, string action = "Index") => RedirectToIndex(characterGroupId.ProjectId, characterGroupId.CharacterGroupId, action);


    private static T FillFromCharacterGroup<T>(T viewModel, CharacterGroup field)
      where T : CharacterGroupViewModelBase
    {
        viewModel.ProjectName = field.Project.ProjectName;
        viewModel.ProjectId = field.Project.ProjectId;
        return viewModel;
    }

    private async Task<EditCharacterGroupViewModel> BuildEditViewModel(
        CharacterGroupFullInfo charGroupFullInfo,
        CharacterGroupIdentification charGroupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(charGroupId.ProjectId);
        return new EditCharacterGroupViewModel
        {
            CharacterGroupId = charGroupId.CharacterGroupId,
            ParentCharacterGroupIdInts = [.. charGroupFullInfo.DirectParentGroupIds.Select(x => x.Id)],
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

    [MasterAuthorize(Permission.CanEditRoles)]
    [HttpGet]
    public Task<ActionResult> MoveUp(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId) => MoveImpl(projectId, charactergroupId, parentCharacterGroupId, currentRootGroupId, -1);

    private async Task<ActionResult> MoveImpl(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId, short direction)
    {

        try
        {
            await characterGroupService.MoveCharacterGroup(new(new(projectId), charactergroupId), new(new(projectId), parentCharacterGroupId), direction);


            return RedirectToIndex(projectId, currentRootGroupId);
        }
        catch
        {
            return RedirectToIndex(projectId, currentRootGroupId);
        }
    }

    [MasterAuthorize(Permission.CanEditRoles)]
    [HttpGet]
    public Task<ActionResult> MoveDown(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId) => MoveImpl(projectId, charactergroupId, parentCharacterGroupId, currentRootGroupId, +1);

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult> Details(int projectId, int characterGroupId)
    {
        var group = await projectRepository.GetGroupAsync(projectId, characterGroupId);
        if (group == null)
        {
            return NotFound();
        }
        var viewModel = new CharacterGroupDetailsViewModel(group, currentUserAccessor.UserIdOrDefault, GroupNavigationPage.Home);
        return View(viewModel);
    }

    protected string GetFullyQualifiedUri(
    string actionName,
    string controllerName,
        object routeValues)
    {
        var url = new Uri(Request.GetDisplayUrl());
        if (url == null)
        {
            throw new InvalidOperationException("Request.Url is unexpectedly null");
        }

        return url.Scheme + "://" + url.Host +
               (url.IsDefaultPort ? "" : $":{url.Port}") +
               Url.Action(actionName, controllerName, routeValues);
    }

}


