using System.Text.Json;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;
using JoinRpg.WebComponents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using MoreLinq;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/roles/{characterGroupId}/[action]")]
public class GameGroupsController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IUriService uriService,
    IUriLocator<UserLinkViewModel> userLinkLocator,
    IProjectMetadataRepository projectMetadataRepository
    ) : ControllerGameBase(projectRepository, projectService)
{
    [HttpGet("~/{projectId}/roles/{characterGroupId?}")]
    [AllowAnonymous]
    public async Task<ActionResult> Index(ProjectIdentification projectId, int? characterGroupId)
    {
        var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

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
              ShowEditControls = field.HasEditRolesAccess(CurrentUserIdOrDefault),
              HasMasterAccess = field.HasMasterAccess(CurrentUserIdOrDefault),
              Data = CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault, projectInfo),
              Details = new CharacterGroupDetailsViewModel(field, CurrentUserIdOrDefault, GroupNavigationPage.Roles),
          });
    }

    [MasterAuthorize]
    [HttpGet("~/{projectId}/roles/{characterGroupId:int}/report")]
    [HttpGet("~/{projectId}/roles/all/report")]
    public async Task<ActionResult> Report(int projectId, int? characterGroupId)
    {
        var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

        if (field == null)
        {
            return NotFound();
        }

        return View(
          new GameRolesReportViewModel
          {
              ProjectId = field.Project.ProjectId,
              Data = CharacterGroupReportViewModel.GetGroups(field),
              Details = new CharacterGroupDetailsViewModel(field, CurrentUserIdOrDefault, GroupNavigationPage.Report),
              CheckinModuleEnabled = field.Project.Details.EnableCheckInModule,
          });
    }

    [HttpGet("~/{projectId}/roles/hot")]
    [AllowAnonymous]
    public Task<ActionResult> Hot(int projectId) => Hot(projectId, null);

    [HttpGet]
    public async Task<ActionResult> Hot(int projectId, int? characterGroupId)
    {
        var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
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
        var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
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
        return CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault, projectInfo)
          .SelectMany(
            g => g.PublicCharacters.Where(ch => ch.IsHot && ch.IsFirstCopy)).Distinct();
    }

    [HttpGet("~/{projectId}/roles/{characterGroupId}/indexjson")]
    [AllowAnonymous]
    public async Task<ActionResult> IndexJson(ProjectIdentification projectId, int characterGroupId)
    {
        var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
        if (field == null)
        {
            return NotFound();

        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var hasMasterAccess = field.HasMasterAccess(CurrentUserIdOrDefault);
        return ReturnJson(new
        {
            field.Project.ProjectId,
            field.Project.ProjectName,
            ShowEditControls = hasMasterAccess,
            Groups = CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault, projectInfo).Select(
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

    [HttpGet("~/{projectId}/roles/json_full")]
    public Task<ActionResult> Json_Full(ProjectIdentification projectId) => AllGroupsJson(projectId, includeSpecial: true);

    [HttpGet("~/{projectId}/roles/json_real")]
    public Task<ActionResult> Json_Real(ProjectIdentification projectId) => AllGroupsJson(projectId, includeSpecial: false);

    private async Task<ActionResult> AllGroupsJson(ProjectIdentification projectId, bool includeSpecial)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        var cached = CheckCache(project.CharacterTreeModifiedAt);
        if (cached)
        {
            return NotModified();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var field = await ProjectRepository.LoadGroupWithTreeSlimAsync(projectId);
        if (field == null)
        {
            return NotFound();
        }
        return ReturnJson(new
        {
            field.Project.ProjectId,
            Groups =
            new CharacterTreeBuilder(field, CurrentUserId, projectInfo).Generate()
              .Where(g => includeSpecial || !g.IsSpecial)
              .Select(
                g =>
                  new
                  {
                      g.CharacterGroupId,
                      g.Name,
                      g.DeepLevel,
                      g.FirstCopy,
                      Path = g.Path.Select(gr => gr.Name),
                      PathIds = g.Path.Select(gr => gr.CharacterGroupId),
                      g.Characters,
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
    public async Task<ActionResult> Edit(int projectId, int characterGroupId)
    {
        var group = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

        if (group == null)
        {
            return NotFound();
        }

        if (group.IsSpecial)
        {
            return Content("Can't edit special group");
        }

        if (group.IsRoot)
        {
            return RedirectToActionPermanent("Index");
        }

        return View(FillFromCharacterGroup(new EditCharacterGroupViewModel
        {
            ParentCharacterGroupIds = group.GetParentGroupsForEdit(),
            Description = group.Description.Contents,
            IsPublic = group.IsPublic,
            Name = group.CharacterGroupName,
            CharacterGroupId = group.CharacterGroupId,
        }, group));
    }


    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Edit(EditCharacterGroupViewModel viewModel)
    {
        var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);
        if (group == null)
        {
            return NotFound();
        }

        if (group.IsRoot)
        {
            return RedirectToActionPermanent("Index");
        }

        if (!ModelState.IsValid)
        {
            return View(FillFromCharacterGroup(viewModel, group));
        }

        if (group.IsSpecial)
        {
            return Content("Can't edit special group");
        }

        try
        {
            ProjectIdentification projectId = new(viewModel.ProjectId);
            await ProjectService.EditCharacterGroup(new CharacterGroupIdentification(projectId, viewModel.CharacterGroupId),
              viewModel.Name, viewModel.IsPublic, [.. viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(projectId)], viewModel.Description);

            return RedirectToIndex(group.ProjectId, group.CharacterGroupId, "Details");
        }
        catch (Exception e)
        {
            ModelState.AddException(e);
            return View(FillFromCharacterGroup(viewModel, group));

        }

    }

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Delete(int projectId, int characterGroupId)
    {
        var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

        if (field == null)
        {
            return NotFound();
        }

        return View(field);
    }


    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
    // ReSharper disable once UnusedParameter.Global
    public async Task<ActionResult> Delete(int projectId, int characterGroupId, IFormCollection collection)
    {
        var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

        if (field == null)
        {
            return NotFound();
        }

        var project = field.Project;
        try
        {
            await ProjectService.DeleteCharacterGroup(projectId, characterGroupId);

            return RedirectToIndex(project);
        }
        catch
        {
            return View(field);
        }
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> AddGroup(int projectid, int charactergroupid)
    {
        var field = await ProjectRepository.GetGroupAsync(projectid, charactergroupid);

        if (field == null)
        {
            return NotFound();
        }

        return View(FillFromCharacterGroup(new AddCharacterGroupViewModel()
        {
            ParentCharacterGroupIds = field.AsPossibleParentForEdit(),
        }, field));
    }

    [HttpPost]
    [MasterAuthorize(Permission.CanEditRoles)]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AddGroup(AddCharacterGroupViewModel viewModel, int charactergroupid)
    {
        var field = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, charactergroupid);
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
            List<CharacterGroupIdentification> parentCharacterGroupIds = [.. viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(new(viewModel.ProjectId))];
            await ProjectService.AddCharacterGroup(
              new(viewModel.ProjectId),
              viewModel.Name, viewModel.IsPublic,
              parentCharacterGroupIds, viewModel.Description);

            return RedirectToRoles(parentCharacterGroupIds.First());
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
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

    private static EditCharacterGroupViewModel FillFromCharacterGroup(
        EditCharacterGroupViewModel viewModel,
        CharacterGroup group)
    {
        viewModel.IsRoot = group.IsRoot;
        viewModel.CreatedAt = group.CreatedAt;
        viewModel.UpdatedAt = group.UpdatedAt;
        viewModel.CreatedBy = group.CreatedBy;
        viewModel.UpdatedBy = group.UpdatedBy;
        _ = FillFromCharacterGroup((CharacterGroupViewModelBase)viewModel, group);
        return viewModel;
    }

    [MasterAuthorize(Permission.CanEditRoles)]
    [HttpGet]
    public Task<ActionResult> MoveUp(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId) => MoveImpl(projectId, charactergroupId, parentCharacterGroupId, currentRootGroupId, -1);

    private async Task<ActionResult> MoveImpl(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId, short direction)
    {

        try
        {
            await ProjectService.MoveCharacterGroup(CurrentUserId, projectId, charactergroupId, parentCharacterGroupId, direction);


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
        var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
        if (group == null)
        {
            return NotFound();
        }
        var viewModel = new CharacterGroupDetailsViewModel(group, CurrentUserIdOrDefault, GroupNavigationPage.Home);
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

    private bool IsClientCached(DateTime contentModified)
    {
        string? header = Request.Headers.IfModifiedSince;

        if (header == null)
        {
            return false;
        }

        return DateTime.TryParse(header, out var isModifiedSince) &&
               isModifiedSince.ToUniversalTime() > contentModified;
    }

    protected bool CheckCache(DateTime characterTreeModifiedAt)
    {
        if (IsClientCached(characterTreeModifiedAt))
        {
            return true;
        }

        Response.Headers.Append("Last-Modified", characterTreeModifiedAt.ToString("R"));
        return false;
    }

}


