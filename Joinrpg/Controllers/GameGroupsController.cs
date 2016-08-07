using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Controllers
{
  public class GameGroupsController : ControllerGameBase
  {
    private readonly IUserRepository _userRepository;

    [HttpGet]
    // GET: GameGroups
    public async Task<ActionResult> Index(int projectId, int? characterGroupId)
    {
      if (characterGroupId == null)
      {
        return await RedirectToProject(projectId);
      }
      
      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, (int) characterGroupId);
      var hasMasterAccess = field.Project.HasMasterAccess(CurrentUserIdOrDefault);
      return WithEntity(field) ?? View(
        new GameRolesViewModel
        {
          ProjectId = field.Project.ProjectId,
          ProjectName = field.Project.ProjectName,
          CharacterGroupId = field.CharacterGroupId,
          ShowEditControls = hasMasterAccess,
          Data = CharacterGroupListViewModel.GetGroups(field, hasMasterAccess)
        });
    }

    [HttpGet, Authorize]
    // GET: GameGroups
    public async Task<ActionResult> Report(int projectId, int? characterGroupId, int? maxDeep)
    {
      if (characterGroupId == null)
      {
        return await RedirectToProject(projectId, "Report");
      }

      ViewBag.MaxDeep = maxDeep ?? 1;

      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, (int)characterGroupId);

      var error = AsMaster(field);
      if (error != null)
      {
        return error;
      }

      var hasMasterAccess = field.HasMasterAccess(CurrentUserId);

      return WithEntity(field) ?? View(
        new GameRolesViewModel
        {
          ProjectId = field.Project.ProjectId,
          ProjectName = field.Project.ProjectName,
          CharacterGroupId = field.CharacterGroupId,
          ShowEditControls = hasMasterAccess,
          Data = CharacterGroupListViewModel.GetGroups(field, hasMasterAccess)
        });
    }

    [HttpGet]
    // GET: GameGroups
    public async Task<ActionResult> Hot(int projectId, int? characterGroupId)
    {
      if (characterGroupId == null)
      {
        return await RedirectToProject(projectId, "Hot");
      }

      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, (int)characterGroupId);
      return WithEntity(field) ?? View(GetHotCharacters(field));
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
      return CharacterGroupListViewModel.GetGroups(field, field.Project.HasMasterAccess(CurrentUserIdOrDefault))
        .SelectMany(
          g => g.PublicCharacters.Where(ch => ch.IsHot && ch.IsFirstCopy));
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
        Groups = CharacterGroupListViewModel.GetGroups(field, hasMasterAccess).Select(
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

      var hasMasterAccess = field.HasMasterAccess(CurrentUserIdOrDefault);
      return ReturnJson(new
      {
        field.Project.ProjectId,
        Groups =
          new CharacterTreeBuilder(field, hasMasterAccess).Generate()
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
        MaxJsonLength = int.MaxValue
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
        PlayerName = ch.HidePlayer ? "скрыто" : ch.Player?.DisplayName,
        PlayerId = ch.HidePlayer ? null : ch.Player?.Id, //TODO Remove
        PlayerLink = (ch.HidePlayer || ch.Player == null) ? null : GetFullyQualifiedUri("Details", "User", new {UserId =  ch.Player?.Id }),
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
        ch.CharacterName
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
        ParentCharacterGroupIds = @group.GetParentGroupsForEdit(),
        Description = new MarkdownViewModel(@group.Description),
        IsPublic = @group.IsPublic,
        Name = @group.CharacterGroupName,
        HaveDirectSlots = GetDirectClaimSettings(@group),
        DirectSlots = Math.Max(@group.AvaiableDirectSlots, 0),
        CharacterGroupId = @group.CharacterGroupId,
        IsRoot = @group.IsRoot,
        ResponsibleMasterId = group.ResponsibleMasterUserId ?? -1,
      }, group));
    }

    private static IEnumerable<MasterListItemViewModel> GetMasters(IClaimSource group, bool includeSelf)
    {
      return group.Project.GetMasterListViewModel()
        .Union(new MasterListItemViewModel()
        {
          Id = "-1",
          Name = "По умолчанию: " + GetDefaultResponsible(group, includeSelf)
        }).OrderByDescending(m => m.Id == "-1").ThenBy(m => m.Name);
    }

    private static string GetDefaultResponsible(IClaimSource group, bool includeSelf)
    {
      var result = group.GetResponsibleMasters(includeSelf)
          .Select(u => u.DisplayName)
          .JoinStrings(", ");
      return string.IsNullOrWhiteSpace(result) ? "Никто" : result;
    }

    private static DirectClaimSettings GetDirectClaimSettings(CharacterGroup group)

    {
      if (!group.HaveDirectSlots)
        return DirectClaimSettings.NoDirectClaims;
      return group.DirectSlotsUnlimited ? DirectClaimSettings.DirectClaimsUnlimited : DirectClaimSettings.DirectClaimsLimited;
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<ActionResult> Edit(EditCharacterGroupViewModel viewModel)
    {
      var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);
      var error = AsMaster(group, acl => acl.CanEditRoles);
      if (error != null)
      {
        return error;
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
          group.ProjectId, group.CharacterGroupId, viewModel.Name, viewModel.IsPublic,
          viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(), viewModel.Description?.Contents, viewModel.HaveDirectSlotsForSave(),
          viewModel.DirectSlotsForSave(), responsibleMasterId);

        return RedirectToIndex(group.Project);
      }
      catch (Exception e)
      {
        ModelState.AddModelError("", e);
        viewModel.IsRoot = group.IsRoot;
        return View(FillFromCharacterGroup(viewModel, group));

      }

    }

    // GET: GameGroups/Delete/5
    [HttpGet,Authorize]
    public async Task<ActionResult> Delete(int projectId, int characterGroupId)
    {
      var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

      return AsMaster(field, pa => pa.CanEditRoles) ?? View(field);
    }

    // POST: GameGroups/Delete/5
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    // ReSharper disable once UnusedParameter.Global
    public async Task<ActionResult> Delete(int projectId, int characterGroupId, FormCollection collection)
    {
      var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

      var error = AsMaster(field, pa => pa.CanEditRoles);
      if (error != null) return error;

      try
      {
        await ProjectService.DeleteCharacterGroup(projectId, field.CharacterGroupId);

        return RedirectToIndex(field.Project);
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
    [Authorize]
    public async Task<ActionResult> AddGroup(int projectid, int charactergroupid)
    {
      var field = await ProjectRepository.GetGroupAsync(projectid, charactergroupid);

      return AsMaster(field, pa => pa.CanEditRoles) ??  View(FillFromCharacterGroup(new AddCharacterGroupViewModel()
      {
        ParentCharacterGroupIds = field.AsPossibleParentForEdit(),
        ResponsibleMasterId = -1
      }, field));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AddGroup(AddCharacterGroupViewModel viewModel, int charactergroupid)
    {
      var field = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, charactergroupid);
      var error = AsMaster(field);
      if (!ModelState.IsValid)
      {
        return View(FillFromCharacterGroup(viewModel, field));
      }
      if (error != null)
      {
        return error;
      }
      try
      {
        var responsibleMasterId = viewModel.ResponsibleMasterId == -1 ? (int?) null : viewModel.ResponsibleMasterId;
        await ProjectService.AddCharacterGroup(
          viewModel.ProjectId, viewModel.Name, viewModel.IsPublic,
          viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(), viewModel.Description.Contents, viewModel.HaveDirectSlotsForSave(),
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
      var error = AsMaster(@group);
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

    [HttpGet,Authorize]
    public async Task<ActionResult> EditSubscribe(int projectId, int characterGroupId)
    {
      var group = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

      var user = await _userRepository.GetWithSubscribe(CurrentUserId);

      return AsMaster(@group) ?? View(new SubscribeSettingsViewModel(user, @group));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<ActionResult> EditSubscribe(SubscribeSettingsViewModel viewModel)
    {
      var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);
      var error = AsMaster(group);
      if (error != null)
      {
        return error;
      }

      try
      {
        await
         ProjectService.UpdateSubscribeForGroup(@group.ProjectId, @group.CharacterGroupId, CurrentUserId,
           viewModel.ClaimStatusChangeValue, viewModel.CommentsValue,
           viewModel.FieldChangeValue, viewModel.MoneyOperationValue);

        return RedirectToIndex(group.Project);
      }
      catch (Exception e)
      {
        ModelState.AddModelError("", e);
        return View(viewModel);
      }

    }
  }
}