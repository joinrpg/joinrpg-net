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
      return WithEntity(field) ?? View(
        new GameRolesViewModel
        {
          ProjectId = field.Project.ProjectId,
          ProjectName = field.Project.ProjectName,
          ShowEditControls = field.Project.GetProjectAcl(CurrentUserIdOrDefault) != null,
          Data = CharacterGroupListViewModel.FromGroup(field)
        });
    }

    [HttpGet]
    public async Task<ActionResult> IndexJson(int projectId, int characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);
      if (field == null)
      {
        return HttpNotFound();
      }

      return Json(
        new
        {
          field.Project.ProjectId,
          field.Project.ProjectName,
          ShowEditControls = field.Project.GetProjectAcl(CurrentUserIdOrDefault) != null,
          Groups =
            CharacterGroupListViewModel.FromGroup(field)
              .PublicGroups.Select(
                g =>
                  new
                  {
                    g.CharacterGroupId,
                    g.Name,
                    g.DeepLevel,
                    g.FirstCopy,
                    g.Description,
                    Path = g.Path.Select(gr => gr.Name),
                    Characters =
                      g.PublicCharacters.Select(
                        ch =>
                          new
                          {
                            p = ch.CharacterId,
                            ch.IsAvailable,
                            ch.IsFirstCopy,
                            ch.CharacterName,
                            ch.Description,
                            PlayerName = ch.Player?.DisplayName,
                            PlayerId = ch.Player?.Id
                          }),
                    CanAddDirectClaim = g.AvaiableDirectSlots != 0
                  }),
        }, JsonRequestBehavior.AllowGet);
    }

    // GET: GameGroups/Edit/5
    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int characterGroupId)
    {
      var group = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);
      
      var user = await _userRepository.GetWithSubscribe(CurrentUserId);

      var error = AsMaster(group, pa => pa.CanChangeFields);
      if (error != null)
      {
        return error;
      }

      return View(new EditCharacterGroupViewModel
      {
        Data = CharacterGroupListViewModel.FromGroupAsMaster(group.Project.RootGroup),
        ProjectId = group.Project.ProjectId,
        ParentCharacterGroupIds = @group.ParentGroups.Select(pg => pg.CharacterGroupId).ToList(),
        Description = @group.Description,
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

    private static IEnumerable<MasterListItemViewModel>  GetMasters(CharacterGroup @group, bool includeSelf)
    {
      return MasterListItemViewModel.FromProject(@group.Project)
        .Union(new MasterListItemViewModel()
        {
          Id = "-1",
          Name = "По умолчанию: " + GetDefaultResponsible(@group, includeSelf)
        });
    }

    private static string GetDefaultResponsible(CharacterGroup group, bool includeSelf)
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
    public ActionResult Delete(int projectId, int characterGroupId)
    {
      return WithGroupAsMaster(projectId, characterGroupId, (project, @group) => View(@group));
    }

    // POST: GameGroups/Delete/5
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    public ActionResult Delete(int projectId, int characterGroupId, FormCollection collection)
    {

      return WithGroupAsMaster(projectId, characterGroupId, (project, @group) =>
      {
        try
        {
          ProjectService.DeleteCharacterGroup(
            projectId, characterGroupId);

          return RedirectToIndex(project);
        }
        catch
        {
          return View(group);
        }
      });
    }

    public GameGroupsController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IUserRepository userRepository)
      : base(userManager, projectRepository, projectService)
    {
      _userRepository = userRepository;
    }

    [HttpGet]
    [Authorize]
    public ActionResult AddGroup(int projectid, int charactergroupid)
    {
      return WithGroupAsMaster(projectid, charactergroupid,
        (project, @group) => View(new AddCharacterGroupViewModel()
        {
          Data = CharacterGroupListViewModel.FromGroupAsMaster(project.RootGroup),
          ProjectId = projectid,
          ParentCharacterGroupIds = new List<int> {charactergroupid},
          Masters = GetMasters(@group, includeSelf: true),
          ResponsibleMasterId = -1
        }));
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

    private ActionResult WithGroupAsMaster(int projectId, int? groupId, Func<Project, CharacterGroup, ActionResult> action)
    {
      var field = groupId == null ? null : ProjectRepository.GetCharacterGroup(projectId, (int)groupId);

      return AsMaster(field, pa => pa.CanChangeFields) ?? action(field.Project, field);
    }

  }
}