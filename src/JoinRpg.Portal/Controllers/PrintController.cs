using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Helpers;
using JoinRpg.Web.Models.Print;
using JoinRpg.WebPortal.Managers.Plots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{projectId}/print/[action]")]
public class PrintController(
    ICharacterRepository characterRepository,
    ICharacterInfoRepository characterInfoRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    CharacterPlotViewService characterPlotViewService,
    JoinrpgMarkdownLinkRendererFactory linkRendererFactory
    ) : JoinControllerGameBase
{
    [HttpGet]
    public async Task<IActionResult> Character(ProjectIdentification projectId, int characterid)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var characterId = new CharacterIdentification(projectId, characterid);
        var character = await characterRepository.GetCharacterWithGroups(projectId, characterid);
        if (character == null)
        {
            return NotFound();
        }
        if (!character.HasAnyAccess(currentUserAccessor.UserIdentificationOrDefault))
        {
            return NoAccesToProjectView(projectInfo, currentUserAccessor);
        }


        var plots = await characterPlotViewService.GetPlotForCharacters([characterId], Domain.Access.CharacterAccessMode.Print);

        var handouts = await characterPlotViewService.GetHandoutsForCharacters([characterId]);

        var characterInfo = await characterInfoRepository.GetCharacterInfo(characterId);

        return View(new PrintCharacterViewModel(
            currentUserAccessor,
            characterInfo,
            character.ToEnvelopeViewModel(projectInfo),
            plots[characterId],
            handouts[characterId],
            await linkRendererFactory.Load(projectId),
            await userRepository.LoadFieldUserLinks(characterInfo)));
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> CharacterList(ProjectIdentification projectId, CompressedIntList characterIds)
        => View(await LoadCharactersToPrint(projectId, characterIds));

    /// <summary>
    /// Печатает для каждого персонажа только заглавную страницу конверта (надпись и чек-лист раздатки),
    /// без полей персонажа и загрузов.
    /// </summary>
    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> HandoutOnly(ProjectIdentification projectId, CompressedIntList characterIds)
        => View(await LoadCharactersToPrint(projectId, characterIds));

    private async Task<PrintCharacterViewModel[]> LoadCharactersToPrint(ProjectIdentification projectId, CompressedIntList characterIds)
    {
        IReadOnlyCollection<CharacterIdentification> characterIdsList = characterIds.ToCharacterIds(projectId);
        var characters = await characterRepository.LoadCharactersWithGroups(characterIdsList);

        // Агрегаты одним запросом на всю пачку — вью-модель печати живёт на них (ADR013).
        var characterInfos = (await characterInfoRepository.GetCharacterInfos(characterIdsList))
            .ToDictionary(c => c.Id);

        var plots = await characterPlotViewService.GetPlotForCharacters(characterIdsList, Domain.Access.CharacterAccessMode.Print);
        var handouts = await characterPlotViewService.GetHandoutsForCharacters(characterIdsList);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var linkRenderer = await linkRendererFactory.Load(projectId);

        // Один запрос на всю пачку печати, а не по персонажу.
        var fieldUsers = await userRepository.LoadFieldUserLinks(characterInfos.Values);

        return
          [.. characters.Select(
            c =>
            {
                var characterId = new CharacterIdentification(c.ProjectId, c.CharacterId);
                return new PrintCharacterViewModel(
                    currentUserAccessor,
                    characterInfos[characterId],
                    // Конверт пока собирается по EF-сущности: в агрегате нет ни названия проживания,
                    // ни телефона игрока.
                    c.ToEnvelopeViewModel(projectInfo),
                    plots[characterId],
                    handouts[characterId],
                    linkRenderer,
                    fieldUsers);
            })];
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        var characters = (await characterRepository.LoadCharactersWithGroups(projectId)).Where(c => c.IsActive).ToList();

        return
          View(new PrintIndexViewModel(projectId, characters.Select(c => c.GetId()).ToArray()));
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> HandoutReport(ProjectIdentification projectid)
    {
        var report = await characterPlotViewService.GetHandoutReport(projectid, PlotVersionFilter.LatestVersion);

        return View(report);
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> Stickers(ProjectIdentification projectId, CompressedIntList characterIds)
    {
        var characters = await characterRepository.LoadCharactersWithGroups(characterIds.ToCharacterIds(projectId));

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var viewModel = characters.Where(c => c.IsActive).Select(c => c.ToEnvelopeViewModel(projectInfo)).ToArray();

        return View(viewModel);
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> EnvelopesC5(ProjectIdentification projectId)
    {
        var characters = await characterRepository.LoadCharactersWithGroups(projectId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var viewModel = characters.Where(c => c.IsActive).Select(c => c.ToEnvelopeViewModel(projectInfo)).ToArray();

        return View(viewModel);
    }
}
