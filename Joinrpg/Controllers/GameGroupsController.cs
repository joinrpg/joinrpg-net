using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;
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
      
      var field = await ProjectRepository.LoadGroupAsync(projectId, (int) characterGroupId);
      var hasMasterAccess = field.Project.HasMasterAccess(CurrentUserIdOrDefault);
      return WithEntity(field) ?? View(
        new GameRolesViewModel
        {
          ProjectId = field.Project.ProjectId,
          ProjectName = field.Project.ProjectName,
          CharacterGroupId = field.CharacterGroupId,
          ShowEditControls = hasMasterAccess,
          Data = CharacterGroupListViewModel.FromGroup(field, hasMasterAccess)
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

      var field = await ProjectRepository.LoadGroupAsync(projectId, (int)characterGroupId);
      return WithEntity(field) ?? View(GetHotCharacters(field));
    }



    [HttpGet]
    public async Task<ActionResult> HotJson(int projectId, int characterGroupId, int? maxCount = null)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);
      if (field == null)
      {
        return HttpNotFound();
      }

      var hotRoles = GetHotCharacters(field).Shuffle().Take(maxCount ?? int.MaxValue);

      return ReturnJson(hotRoles.Select(ConvertCharacterToJson));
    }

    private IEnumerable<CharacterViewModel> GetHotCharacters(CharacterGroup field)
    {
      return CharacterGroupListViewModel.FromGroup(field, field.Project.HasMasterAccess(CurrentUserIdOrDefault))
        .PublicGroups.SelectMany(
          g => g.PublicCharacters.Where(ch => ch.IsHot));
    }

    [HttpGet]
    public async Task<ActionResult> IndexJson(int projectId, int characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);
      if (field == null)
      {
        return HttpNotFound();

      }

      var hasMasterAccess = field.Project.HasMasterAccess(CurrentUserIdOrDefault);
      return ReturnJson(new
      {
        field.Project.ProjectId,
        field.Project.ProjectName,
        ShowEditControls = hasMasterAccess,
        Groups =
          CharacterGroupListViewModel.FromGroup(field, hasMasterAccess)
            .PublicGroups.Select(
              g =>
                new
                {
                  g.CharacterGroupId,
                  g.Name,
                  g.DeepLevel,
                  g.FirstCopy,
                  Description = g.Description?.ToHtmlString(),
                  Path = g.Path.Select(gr => gr.Name),
                  Characters = g.PublicCharacters.Select(ConvertCharacterToJson),
                  CanAddDirectClaim = g.AvaiableDirectSlots != 0,
                  DirectClaimsCount = g.AvaiableDirectSlots,
                }),
      });
    }

    private ActionResult ReturnJson(object data)
    {
      ControllerContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
      return Json(
        data, JsonRequestBehavior.AllowGet);
    }

    private object ConvertCharacterToJson(CharacterViewModel ch)
    {
      return new
      {
        p = ch.CharacterId,
        ch.IsAvailable,
        ch.IsFirstCopy,
        ch.CharacterName,
        Description = ch.Description?.ToHtmlString(),
        PlayerName = ch.HidePlayer ? "скрыто" : ch.Player?.DisplayName,
        PlayerId = ch.Player?.Id,
        ch.ActiveClaimsCount,
        ClaimLink =
          ch.IsAvailable
            ? new UrlHelper(ControllerContext.RequestContext).Action("AddForCharacter", "Claim",
              new {ch.ProjectId, ch.CharacterId})
            : null,
      };
    }

    // GET: GameGroups/Edit/5
    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int characterGroupId)
    {
      var group = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);
      
      var user = await _userRepository.GetWithSubscribe(CurrentUserId);

      var error = AsMaster(group, pa => pa.CanEditRoles);
      if (error != null)
      {
        return error;
      }

      return View(new EditCharacterGroupViewModel
      {
        Data = CharacterGroupListViewModel.FromGroupAsMaster(group.Project.RootGroup),
        ProjectId = group.Project.ProjectId,
        ParentCharacterGroupIds = @group.ParentGroups.Select(pg => pg.CharacterGroupId).ToList(),
        Description = new MarkdownViewModel(@group.Description),
        IsPublic = @group.IsPublic,
        Name = @group.CharacterGroupName,
        HaveDirectSlots = GetDirectClaimSettings(@group),
        DirectSlots = Math.Max(@group.AvaiableDirectSlots, 0),
        CharacterGroupId = @group.CharacterGroupId,
        IsRoot = @group.IsRoot,
        Subscribe = new SubscribeSettingsViewModel(user, group),
        ResponsibleMasterId = group.ResponsibleMasterUserId ?? -1,
        Masters = GetMasters(@group, false)
      });
    }

    private static IEnumerable<MasterListItemViewModel>  GetMasters(IClaimSource @group, bool includeSelf)
    {
      return MasterListItemViewModel.FromProject(@group.Project)
        .Union(new MasterListItemViewModel()
        {
          Id = "-1",
          Name = "По умолчанию: " + GetDefaultResponsible(@group, includeSelf)
        }).OrderByDescending(m => m.Id == "-1").ThenBy(m => m.Name);
    }

    private static string GetDefaultResponsible(IClaimSource group, bool includeSelf)
    {
      var result = group.GetResponsibleMasters(includeSelf)
          .Select(u => u.DisplayName)
          .Join(", ");
      return string.IsNullOrWhiteSpace(result) ? "Никто" : result;
    }

    private static DirectClaimSettings GetDirectClaimSettings(CharacterGroup group)

    {
      if (!@group.HaveDirectSlots)
        return DirectClaimSettings.NoDirectClaims;
      return @group.DirectSlotsUnlimited ? DirectClaimSettings.DirectClaimsUnlimited : DirectClaimSettings.DirectClaimsLimited;
    }

    // POST: GameGroups/Edit/5
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<ActionResult> Edit(EditCharacterGroupViewModel viewModel)
    {
      CharacterGroup group = await ProjectRepository.LoadGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);
      var error = AsMaster(group);
      if (error != null)
      {
        return error;
      }

      try
      {
        var responsibleMasterId = viewModel.ResponsibleMasterId == -1 ? (int?) null : viewModel.ResponsibleMasterId;
        await ProjectService.EditCharacterGroup(
          group.ProjectId, @group.CharacterGroupId, viewModel.Name, viewModel.IsPublic,
          viewModel.ParentCharacterGroupIds, viewModel.Description?.Contents, viewModel.HaveDirectSlotsForSave(),
          viewModel.DirectSlotsForSave(), responsibleMasterId);

        await
          ProjectService.UpdateSubscribeForGroup(@group.ProjectId, @group.CharacterGroupId, CurrentUserId,
            viewModel.Subscribe.ClaimStatusChangeValue, viewModel.Subscribe.CommentsValue,
            viewModel.Subscribe.FieldChangeValue);


        return RedirectToIndex(group.Project);
      }
      catch
      {
        return View(viewModel);
      }

    }

    // GET: GameGroups/Delete/5
    [HttpGet,Authorize]
    public async Task<ActionResult> Delete(int projectId, int characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);

      return AsMaster(field, pa => pa.CanEditRoles) ?? View(field);
    }

    // POST: GameGroups/Delete/5
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    // ReSharper disable once UnusedParameter.Global
    public async Task<ActionResult> Delete(int projectId, int characterGroupId, FormCollection collection)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);

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
      var field = await ProjectRepository.LoadGroupAsync(projectid, charactergroupid);

      return AsMaster(field, pa => pa.CanEditRoles) ??  View(new AddCharacterGroupViewModel()
      {
        Data = CharacterGroupListViewModel.FromGroupAsMaster(field.Project.RootGroup),
        ProjectId = projectid,
        ParentCharacterGroupIds = new List<int> {charactergroupid},
        Masters = GetMasters(field, includeSelf: true),
        ResponsibleMasterId = -1
      });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AddGroup(AddCharacterGroupViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = AsMaster(project);
      if (error != null)
      {
        return error;
      }
      try
      {
        var responsibleMasterId = viewModel.ResponsibleMasterId == -1 ? (int?) null : viewModel.ResponsibleMasterId;
        await ProjectService.AddCharacterGroup(
          viewModel.ProjectId, viewModel.Name, viewModel.IsPublic,
          viewModel.ParentCharacterGroupIds, viewModel.Description.Contents, viewModel.HaveDirectSlotsForSave(),
          viewModel.DirectSlotsForSave(), responsibleMasterId);

        return RedirectToIndex(project);
      }
      catch
      {
        return View(viewModel);
      }
    }

    public Task<ActionResult> MoveUp(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId)
    {
      return MoveImpl(projectId, charactergroupId, parentCharacterGroupId, currentRootGroupId, -1);
    }

    private async Task<ActionResult> MoveImpl(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId, int direction)
    {
      var group = await ProjectRepository.LoadGroupAsync(projectId, charactergroupId);
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
  }
}