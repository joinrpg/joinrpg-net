using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces; 
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Controllers
{
  public class GameGroupsController : ControllerGameBase
  {
    private readonly IUserRepository _userRepository;

    [HttpGet]
    public async Task<ActionResult> Index(int projectId, int? characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

      if (field == null) return HttpNotFound();
      
      return View(
        new GameRolesViewModel
        {
          ProjectId = field.Project.ProjectId,
          ProjectName = field.Project.ProjectName,
          ShowEditControls = field.HasEditRolesAccess(CurrentUserIdOrDefault),
          HasMasterAccess = field.HasMasterAccess(CurrentUserIdOrDefault),
          Data = CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault),
          Details = new CharacterGroupDetailsViewModel(field, CurrentUserIdOrDefault, GroupNavigationPage.Roles),
        });
    }

    [HttpGet, MasterAuthorize]
    public async Task<ActionResult> Report(int projectId, int? characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

      if (field == null) return HttpNotFound();

      return View(
        new GameRolesReportViewModel
        {
          ProjectId = field.Project.ProjectId,
          Data = CharacterGroupReportViewModel.GetGroups(field),
          Details = new CharacterGroupDetailsViewModel(field, CurrentUserIdOrDefault, GroupNavigationPage.Report),
          CheckinModuleEnabled = field.Project.Details.EnableCheckInModule,
        });
    }

    [HttpGet]
    public async Task<ActionResult> Hot(int projectId, int? characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
      if (field == null) return HttpNotFound();
      return  View(GetHotCharacters(field));
    }

    [HttpGet]
    public async Task<ActionResult> HotJson(int projectId, int characterGroupId, int? maxCount = null)
    {
      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
      if (field == null)
      {
        return HttpNotFound();
      }

      var hotRoles = GetHotCharacters(field).Shuffle().Take(maxCount ?? int.MaxValue);

      return ReturnJson(hotRoles.Select(ConvertCharacterToJson));
    }

    private IEnumerable<CharacterViewModel> GetHotCharacters(CharacterGroup field)
    {
      return CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault)
        .SelectMany(
          g => g.PublicCharacters.Where(ch => ch.IsHot && ch.IsFirstCopy)).Distinct();
    }

    [HttpGet, Compress]
    public async Task<ActionResult> IndexJson(int projectId, int characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
      if (field == null)
      {
        return HttpNotFound();

      }

      var hasMasterAccess = field.HasMasterAccess(CurrentUserIdOrDefault);
      return ReturnJson(new
      {
        field.Project.ProjectId,
        field.Project.ProjectName,
        ShowEditControls = hasMasterAccess,
        Groups = CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault).Select(
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
                  CanAddDirectClaim = g.IsAcceptingClaims,
                  DirectClaimsCount = g.AvaiableDirectSlots,
                  DirectClaimLink = g.IsAcceptingClaims ? GetFullyQualifiedUri("AddForGroup", "Claim", new {field.ProjectId, g.CharacterGroupId}) : null,
                }),
      });
    }

    [HttpGet, Compress]
    public async Task<ActionResult> AllGroupsJson(int projectId, bool includeSpecial)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var cached = CheckCache(project.CharacterTreeModifiedAt);
      if (cached)
      {
        return NotModified();
      }

      var field = await ProjectRepository.LoadGroupWithTreeSlimAsync(projectId);
      if (field == null)
      {
        return HttpNotFound();
      }
      return ReturnJson(new
      {
        field.Project.ProjectId,
        Groups =
          new CharacterTreeBuilder(field, CurrentUserId).Generate()
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
                  Characters = g.Characters.Select(ConvertCharacterToJsonSlim),
                }),
      });
    }

    private ActionResult ReturnJson(object data)
    {
      ControllerContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
      return new JsonResult()
      {
        Data = data,
        ContentEncoding = Encoding.UTF8,
        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        MaxJsonLength = int.MaxValue,
      };
    }

    private object ConvertCharacterToJson(CharacterViewModel ch)
    {
      return new
      {
        ch.CharacterId, //TODO Remove
        CharacterLink = GetFullyQualifiedUri("Details", "Character", new { ch.CharacterId }),
        ch.IsAvailable,
        ch.IsFirstCopy,
        ch.CharacterName,
        Description = ch.Description?.ToHtmlString(),
        PlayerName = ch.HidePlayer ? "скрыто" : ch.Player?.GetDisplayName(),
        PlayerId = ch.HidePlayer ? null : ch.Player?.UserId, //TODO Remove
        PlayerLink = (ch.HidePlayer || ch.Player == null) ? null : GetFullyQualifiedUri("Details", "User", new {ch.Player?.UserId }),
        ch.ActiveClaimsCount,
        ClaimLink =
          ch.IsAvailable
            ? GetFullyQualifiedUri("AddForCharacter", "Claim", new {ch.ProjectId, ch.CharacterId})
            : null,
      };
    }

    private object ConvertCharacterToJsonSlim(CharacterLinkViewModel ch)
    {
      return new
      {
        ch.CharacterId,
        ch.IsAvailable,
        ch.IsFirstCopy,
        ch.CharacterName,
      };
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int characterGroupId)
    {
      var group = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

      var error = AsMaster(group, pa => pa.CanEditRoles);
      if (error != null)
      {
        return error;
      }

      if (group.IsSpecial)
      {
        return Content("Can't edit special group");
      }

      return View(FillFromCharacterGroup(new EditCharacterGroupViewModel
      {
        ParentCharacterGroupIds = group.GetParentGroupsForEdit(),
        Description = group.Description.Contents,
        IsPublic = group.IsPublic,
        Name = group.CharacterGroupName,
        HaveDirectSlots = GetDirectClaimSettings(group),
        DirectSlots = Math.Max(group.AvaiableDirectSlots, 0),
        CharacterGroupId = group.CharacterGroupId,
        IsRoot = group.IsRoot,
        ResponsibleMasterId = group.ResponsibleMasterUserId ?? -1,
        CreatedAt = group.CreatedAt,
        UpdatedAt = group.UpdatedAt,
        CreatedBy = group.CreatedBy,
        UpdatedBy = group.UpdatedBy,
    }, group));
    }

    private static IEnumerable<MasterListItemViewModel> GetMasters(IClaimSource group, bool includeSelf)
    {
      return group.Project.GetMasterListViewModel()
        .Union(new MasterListItemViewModel()
        {
          Id = "-1",
          Name = "По умолчанию", // TODO Temporary disabled as shown in hot profiles + GetDefaultResponsible(group, includeSelf)
        }).OrderByDescending(m => m.Id == "-1").ThenBy(m => m.Name);
    }

    private static string GetDefaultResponsible(IClaimSource group, bool includeSelf)
    {
      var result = ResponsibleMasterExtensions.GetResponsibleMasters(@group, includeSelf)
          .Select(u => u.GetDisplayName())
          .JoinStrings(", ");
      return string.IsNullOrWhiteSpace(result) ? "Никто" : result;
    }

    private static DirectClaimSettings GetDirectClaimSettings(CharacterGroup group)

    {
      if (!group.HaveDirectSlots)
        return DirectClaimSettings.NoDirectClaims;
      return group.DirectSlotsUnlimited ? DirectClaimSettings.DirectClaimsUnlimited : DirectClaimSettings.DirectClaimsLimited;
    }

    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Edit(EditCharacterGroupViewModel viewModel)
    {
      var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);
      if (group == null)
      {
        return HttpNotFound();
      }

      if (!ModelState.IsValid && !group.IsRoot) //TODO: We can't actually validate root group — too many errors.
      {
        viewModel.IsRoot = group.IsRoot;
        return View(FillFromCharacterGroup(viewModel, group));
      }

      if (group.IsSpecial)
      {
        return Content("Can't edit special group");
      }

      try
      {
        var responsibleMasterId = viewModel.ResponsibleMasterId == -1 ? (int?) null : viewModel.ResponsibleMasterId;
        await ProjectService.EditCharacterGroup(
          group.ProjectId, 
          CurrentUserId,
          group.CharacterGroupId, viewModel.Name, viewModel.IsPublic,
          viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(), viewModel.Description, viewModel.HaveDirectSlotsForSave(),
          viewModel.DirectSlotsForSave(), responsibleMasterId);

          return RedirectToIndex(group.ProjectId, group.CharacterGroupId, "Details");
      }
      catch (Exception e)
      {
        ModelState.AddException(e);
        viewModel.IsRoot = group.IsRoot;
        return View(FillFromCharacterGroup(viewModel, group));

      }

    }

    [HttpGet,MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Delete(int projectId, int characterGroupId)
    {
      var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

      if (field == null) return HttpNotFound();

      return View(field);
    }


    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
    // ReSharper disable once UnusedParameter.Global
    public async Task<ActionResult> Delete(int projectId, int characterGroupId, FormCollection collection)
    {
      var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

      if (field == null) return HttpNotFound();

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

    public GameGroupsController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IUserRepository userRepository, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      _userRepository = userRepository;
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> AddGroup(int projectid, int charactergroupid)
    {
      var field = await ProjectRepository.GetGroupAsync(projectid, charactergroupid);

      if (field == null) return HttpNotFound();

      return View(FillFromCharacterGroup(new AddCharacterGroupViewModel()
      {
        ParentCharacterGroupIds = field.AsPossibleParentForEdit(),
        ResponsibleMasterId = -1,
      }, field));
    }

    [HttpPost]
    [MasterAuthorize(Permission.CanEditRoles)]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AddGroup(AddCharacterGroupViewModel viewModel, int charactergroupid)
    {
      var field = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, charactergroupid);
      if (field == null) return HttpNotFound();
      if (!ModelState.IsValid)
      {
        return View(FillFromCharacterGroup(viewModel, field));
      }
      
      try
      {
        var responsibleMasterId = viewModel.ResponsibleMasterId == -1 ? (int?) null : viewModel.ResponsibleMasterId;
        await ProjectService.AddCharacterGroup(
          viewModel.ProjectId,
          viewModel.Name, viewModel.IsPublic,
          viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(), viewModel.Description, viewModel.HaveDirectSlotsForSave(),
          viewModel.DirectSlotsForSave(), responsibleMasterId);

        return RedirectToIndex(field.ProjectId, viewModel.ParentCharacterGroupIds.GetUnprefixedGroups().First());
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(FillFromCharacterGroup(viewModel, field));
      }
    }

    private static T FillFromCharacterGroup<T>(T viewModel, IClaimSource field)
      where T: CharacterGroupViewModelBase
    {
      viewModel.Masters = GetMasters(field, includeSelf: true);
      viewModel.ProjectName = field.Project.ProjectName;
      viewModel.ProjectId = field.Project.ProjectId;
      return viewModel;
    }

    public Task<ActionResult> MoveUp(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId)
    {
      return MoveImpl(projectId, charactergroupId, parentCharacterGroupId, currentRootGroupId, -1);
    }

    private async Task<ActionResult> MoveImpl(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId, short direction)
    {
      var group = await ProjectRepository.GetGroupAsync(projectId, charactergroupId);
      var error = AsMaster(group);
      if (error != null)
      {
        return error;
      }

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

    public Task<ActionResult> MoveDown(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId)
    {
      return MoveImpl(projectId, charactergroupId, parentCharacterGroupId, currentRootGroupId, +1);
    }

    [HttpGet,MasterAuthorize()]
    public async Task<ActionResult> EditSubscribe(int projectId, int characterGroupId)
    {
      var group = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

      var user = await _userRepository.GetWithSubscribe(CurrentUserId);

      return AsMaster(group) ?? View(new SubscribeSettingsViewModel(user, group));
    }

    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize()]
    public async Task<ActionResult> EditSubscribe(SubscribeSettingsViewModel viewModel)
    {
      var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);
      
      if (group == null)
      {
        return HttpNotFound();
      }

        var user = await _userRepository.GetWithSubscribe(CurrentUserId);

            var serverModel = new SubscribeSettingsViewModel(user, group);

        serverModel.OrSetIn(viewModel);

      try
      {
          await
              ProjectService.UpdateSubscribeForGroup(new SubscribeForGroupRequest
              {
                  CharacterGroupId = group.CharacterGroupId,
                  ProjectId = group.ProjectId,
              }.AssignFrom(serverModel.GetOptionsToSubscribeDirectly()));

        return RedirectToIndex(group.Project);
      }
      catch (Exception e)
      {
        ModelState.AddException(e);
        return View(serverModel);
      }

    }

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult> Details(int projectId, int characterGroupId)
    {
      var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      if (group == null)
      {
        return HttpNotFound();
      }
      var viewModel = new CharacterGroupDetailsViewModel(group, CurrentUserIdOrDefault, GroupNavigationPage.Home);
      return View(viewModel);
    }
  }
}
