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
      var viewModel = new CharacterDetailsViewModel()
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
    public ActionResult Details(int projectId, int characterId, FormCollection formCollection)
    {
      return WithCharacter(projectId, characterId, (project, character) =>
      {
        try
        {
          ProjectService.SaveCharacterFields(projectId, characterId, CurrentUserId, GetCharacterFieldValuesFromPost(formCollection.ToDictionary()));
          return RedirectToAction("Details", new {projectId, characterId});
        }
        catch
        {
          return Details(projectId, characterId);
        }
      });
      
    }

    private static IDictionary<int,string> GetCharacterFieldValuesFromPost(Dictionary<string, string> post)
    {
      var prefix = "field.field_";
      return post.Keys.UnprefixNumbers(prefix).ToDictionary(fieldClientId => fieldClientId, fieldClientId => post[prefix + fieldClientId]);
    }
  }
}