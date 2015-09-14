using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class CharacterController : Common.ControllerGameBase
  {
    // GET: Character
    public CharacterController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService) : base(userManager, projectRepository, projectService)
    {
    }

    [HttpGet]
    public ActionResult Details(int projectid, int characterid)
    {
      return WithCharacter(projectid, characterid, ShowCharacter);
    }

    private ActionResult ShowCharacter(Project project, Character character)
    {
      var hasMasterAccess = project.HasAccess(CurrentUserIdOrDefault);
      var approvedClaimUser = character.ApprovedClaim?.Player;
      var viewModel = new CharacterDetailsViewModel
      {
        CharacterName = character.CharacterName,
        Description = character.Description.ToHtmlString(),
        ApprovedClaimId = character.ApprovedClaim?.ClaimId,
        ApprovedClaimUser = approvedClaimUser,
        CanAddClaim = character.IsAvailable,
        DiscussedClaims = LoadIfMaster(project, () => character.Claims.Where(claim => claim.IsInDiscussion)),
        RejectedClaims = LoadIfMaster(project, () => character.Claims.Where(claim => !claim.IsActive)),
        CharacterFields = character.Fields().Select(pair => pair.Value),
        ProjectName = project.ProjectName,
        ProjectId = project.ProjectId,
        CharacterId = character.CharacterId,
        HasPlayerAccessToCharacter = hasMasterAccess || approvedClaimUser?.UserId == CurrentUserIdOrDefault,
        HasMasterAccess = hasMasterAccess
      };
      return View(viewModel);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public ActionResult Details(int projectId, int characterId, string characterName, FormCollection formCollection)
    {
      return WithCharacter(projectId, characterId, (project, character) =>
      {
        try
        {
          ProjectService.SaveCharacterFields(projectId, characterId, CurrentUserId, characterName, GetCharacterFieldValuesFromPost(formCollection.ToDictionary()));
          return RedirectToAction("Details", new {projectId, characterId});
        }
        catch
        {
          return Details(projectId, characterId);
        }
      });
    }

    [HttpGet]
    [Authorize]
    public ActionResult Create(int projectid, int charactergroupid)
    {
      return WithGroupAsMaster(projectid, charactergroupid,
        (project, @group) => View(new AddCharacterViewModel()
        {
          Data = CharacterGroupListViewModel.FromGroupAsMaster(project.RootGroup),
          ProjectId = projectid,
          ParentCharacterGroupIds = new List<int> { charactergroupid }
        }));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult Create(AddCharacterViewModel viewModel)
    {
      var project1 = ProjectRepository.GetProject(viewModel.ProjectId);
      return AsMaster(project1, acl => true) ?? ((Func<Project, ActionResult>) (project =>
      {
        try
        {
          ProjectService.AddCharacter(
            viewModel.ProjectId,
            viewModel.Name, viewModel.IsPublic, viewModel.ParentCharacterGroupIds, viewModel.IsAcceptingClaims,
            viewModel.Description);

          return RedirectToIndex(project);
        }
        catch
        {
          return View(viewModel);
        }
      }))(project1);
    }
  }
}