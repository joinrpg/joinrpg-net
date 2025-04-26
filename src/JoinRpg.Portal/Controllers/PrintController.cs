using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Print;
using JoinRpg.WebPortal.Managers.Plots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{projectId}/print/[action]")]
public class PrintController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IPlotRepository plotRepository,
    ICharacterRepository characterRepository,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    CharacterPlotViewService characterPlotViewService
    ) : Common.ControllerGameBase(projectRepository, projectService, userRepository)
{
    [HttpGet]
    public async Task<IActionResult> Character(int projectId, int characterid)
    {
        var characterId = new CharacterIdentification(projectId, characterid);
        var character = await characterRepository.GetCharacterWithGroups(projectId, characterid);
        if (character == null)
        {
            return NotFound();
        }
        if (!character.HasAnyAccess(currentUserAccessor.UserIdOrDefault))
        {
            return NoAccesToProjectView(character.Project);
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectId));
        var plots = await characterPlotViewService.GetPlotForCharacters([characterId], Domain.Access.CharacterAccessMode.Print);

        var handouts = await characterPlotViewService.GetHandoutsForCharacters([characterId]);

        return View(new PrintCharacterViewModel(currentUserAccessor, character, plots[characterId], projectInfo, handouts[characterId]));
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> CharacterList(ProjectIdentification projectId, CompressedIntList characterIds)
    {
        IReadOnlyCollection<CharacterIdentification> characterIdsList = characterIds.ToCharacterIds(projectId);
        var characters = await characterRepository.LoadCharactersWithGroups(characterIdsList);

        var plots = await characterPlotViewService.GetPlotForCharacters(characterIdsList, Domain.Access.CharacterAccessMode.Print);
        var handouts = await characterPlotViewService.GetHandoutsForCharacters(characterIdsList);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var viewModel =
          characters.Select(
            c =>
            {
                var characterId = new CharacterIdentification(c.ProjectId, c.CharacterId);
                return new PrintCharacterViewModel(currentUserAccessor, c, plots[characterId], projectInfo, handouts[characterId]);
            }).ToArray();

        return View(viewModel);
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        var characters = (await characterRepository.LoadCharactersWithGroups(projectId)).Where(c => c.IsActive).ToList();

        return
          View(new PrintIndexViewModel(projectId, characters.Select(c => c.CharacterId).ToArray()));
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> HandoutReport(ProjectIdentification projectid)
    {
        var plotElements =
          await plotRepository.GetActiveHandouts(projectid);

        var characters = (await characterRepository.LoadCharactersWithGroups(projectid)).Where(c => c.IsActive).ToList();

        return View(new HandoutReportViewModel(plotElements, characters));
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> Stickers(ProjectIdentification projectId, CompressedIntList characterIds)
    {
        var characters = await characterRepository.LoadCharactersWithGroups(characterIds.ToCharacterIds(projectId));

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var viewModel = characters.Select(c => c.ToEnvelopeViewModel(projectInfo)).ToArray();

        return View(viewModel);
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> EnvelopesC5(ProjectIdentification projectId)
    {
        var characters = await characterRepository.LoadCharactersWithGroups(projectId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var viewModel = characters.Select(c => c.ToEnvelopeViewModel(projectInfo)).ToArray();

        return View(viewModel);
    }
}
