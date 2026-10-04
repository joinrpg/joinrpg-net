using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Helpers;
using JoinRpg.Web.Models.Print;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.Plots;
using JoinRpg.WebPortal.Managers.Print;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{projectId}/print/[action]")]
public class PrintController(
    ICharacterInfoRepository characterInfoRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    CharacterPlotViewService characterPlotViewService,
    PrintViewService printViewService,
    JoinrpgMarkdownLinkRendererFactory linkRendererFactory
    ) : JoinControllerGameBase
{
    [HttpGet]
    public async Task<IActionResult> Character(ProjectIdentification projectId, int characterid)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var characterId = new CharacterIdentification(projectId, characterid);
        var character = await characterInfoRepository.GetCharacterInfoOrDefault(characterId, projectInfo);
        if (character is null)
        {
            return NotFound();
        }
        if (!AccessArgumentsFactory.Create(character, currentUserAccessor).AnyAccessToCharacter)
        {
            return NoAccesToProjectView(projectInfo, currentUserAccessor);
        }

        var plots = await characterPlotViewService.GetPlotForCharacters([characterId], CharacterAccessMode.Print);

        var handouts = await characterPlotViewService.GetHandoutsForCharacters([characterId]);

        return View(new PrintCharacterViewModel(
            currentUserAccessor,
            character,
            await printViewService.GetEnvelope(character),
            plots[characterId],
            handouts[characterId],
            await linkRendererFactory.Load(projectId),
            await userRepository.LoadFieldUserLinks(character)));
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

        // Агрегаты одним запросом на всю пачку — на них живёт и вью-модель печати, и конверт (ADR013).
        var characters = await characterInfoRepository.GetCharacterInfos(characterIdsList);

        var plots = await characterPlotViewService.GetPlotForCharacters(characterIdsList, CharacterAccessMode.Print);
        var handouts = await characterPlotViewService.GetHandoutsForCharacters(characterIdsList);

        var linkRenderer = await linkRendererFactory.Load(projectId);

        // Эти два тоже по одному запросу на пачку, а не на персонажа.
        var fieldUsers = await userRepository.LoadFieldUserLinks(characters);
        var envelopes = (await printViewService.GetEnvelopes(characters)).ToDictionary(e => e.CharacterId);

        return
          [.. characters.Select(
            character => new PrintCharacterViewModel(
                currentUserAccessor,
                character,
                envelopes[character.Id],
                plots[character.Id],
                handouts[character.Id],
                linkRenderer,
                fieldUsers))];
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        // Странице нужны только идентификаторы ссылок, поэтому лёгкая проекция, а не агрегаты.
        var characters = await characterInfoRepository.GetCharactersForList(projectId, CharacterStatusSpec.Active);

        return View(new PrintIndexViewModel(projectId, [.. characters.Select(character => character.Id)]));
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
        => View(await printViewService.GetEnvelopesForActiveCharacters(characterIds.ToCharacterIds(projectId)));

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> EnvelopesC5(ProjectIdentification projectId)
        => View(await printViewService.GetEnvelopesForActiveCharacters(projectId));
}
