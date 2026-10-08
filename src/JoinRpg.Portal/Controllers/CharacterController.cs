using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Helpers;
using JoinRpg.WebPortal.Managers.Plots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/character/{characterid}/[action]")]
public class CharacterController(
    IProjectRepository projectRepository,
    ICharacterRepository characterRepository,
    ICharacterInfoRepository characterInfoRepository,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUser,
    IUserRepository userRepository,
    CharacterPlotViewService characterPlotViewService,
    JoinrpgMarkdownLinkRendererFactory linkRendererFactory
        ) : JoinControllerGameBase
{

    [Obsolete]
    private int CurrentUserId => currentUser.UserId;

    [HttpGet("~/{projectId}/character/{characterid}/")]
    [HttpGet("~/{projectId}/character/{characterid}/details")]
    [AllowAnonymous]
    public async Task<ActionResult> Details(int projectid, int characterid)
    {
        var field = await characterRepository.GetCharacterWithGroups(projectid, characterid);
        return field is null ? NotFound() : await ShowCharacter(field);
    }

    private async Task<ActionResult> ShowCharacter(Character character)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(character.ProjectId));

        var accessArguments = AccessArgumentsFactory.Create(character, currentUser, projectInfo);
        if (!accessArguments.CanViewCharacterAtAll)
        {
            return NotFound();
        }

        var plots = await characterPlotViewService.GetPlotsForCharacter(character.GetId());

        // Загрузка данных для рендерера — дорогая (персонажи проекта с заявками и игроками, #4992),
        // поэтому платим за неё только когда вводные реально показываются: пустой список — значит
        // доступа к сюжету нет и разворачивать нечего.
        var linkRenderer = plots.Count > 0
            ? await linkRendererFactory.Load(new ProjectIdentification(character.ProjectId))
            : JoinrpgMarkdownLinkRendererFactory.NoDirectives;

        return View("Details",
            new CharacterDetailsViewModel(currentUser,
                character,
                await characterInfoRepository.GetCharacterInfo(character.GetId()),
                plots,
                linkRenderer,
                projectInfo,
                await userRepository.LoadFieldUserLinks(character, projectInfo)));
    }

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Edit(ProjectIdentification projectId, int characterId)
    {
        var field = await characterRepository.GetCharacterWithDetails(projectId, characterId);
        var characterInfo = await characterInfoRepository.GetCharacterInfo(new CharacterIdentification(projectId, characterId));
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return View(new EditCharacterViewModel()
        {
            ProjectId = field.ProjectId,
            CharacterId = field.CharacterId,
            ProjectName = projectInfo.ProjectName,
            CharacterTypeInfo = characterInfo.CharacterTypeInfo,
            Name = field.CharacterName,
            ParentCharacterGroupIds = [.. field.GetDirectNonSpecialGroupIds(projectInfo)],
        }.Fill(field, characterInfo, currentUser.UserIdentification, projectInfo, await userRepository.LoadFieldUserLinks(field, projectInfo)));
    }

    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditCharacterViewModel viewModel)
    {
        var field =
             await characterRepository.GetCharacterAsync(viewModel.ProjectId, viewModel.CharacterId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));

        // У view-модели AllowToSetGroups при POST не заполнен (только Fill на GET), поэтому её
        // IValidatableObject-проверка не срабатывает: пустой список групп доезжал до сервиса
        // и падал там JoinValidationException (#5323). Ловим здесь, по метаданным проекта.
        ValidateCharacterGroups(projectInfo, viewModel);

        try
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel.Fill(field, await characterInfoRepository.GetCharacterInfo(field.GetId()), currentUser.UserIdentification, projectInfo, await userRepository.LoadFieldUserLinks(field, projectInfo)));
            }

            await characterService.EditCharacter(
                new EditCharacterRequest(
                    new CharacterIdentification(viewModel.ProjectId, viewModel.CharacterId),
                    ParentCharacterGroupIds: [.. viewModel.ParentCharacterGroupIds],
                    CharacterTypeInfo: viewModel.CharacterTypeInfo,
                    FieldValues: Request.GetFieldsToSetFromPost(projectInfo, FieldValueViewModel.HtmlIdPrefix))
                );

            return RedirectToAction("Details",
                new { viewModel.ProjectId, viewModel.CharacterId });
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return View(viewModel.Fill(field, await characterInfoRepository.GetCharacterInfo(field.GetId()), currentUser.UserIdentification, projectInfo, await userRepository.LoadFieldUserLinks(field, projectInfo)));
        }
    }

    [HttpGet("~/{ProjectId}/character/create")]
    [MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Create(ProjectIdentification ProjectId, int? charactergroupid, bool continueCreating = false)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(ProjectId);

        CharacterGroupIdentification targetGroupId;

        if (charactergroupid is null || charactergroupid == 0)
        {
            targetGroupId = projectInfo.GroupTree.RootGroupId;
        }
        else
        {
            targetGroupId = new CharacterGroupIdentification(ProjectId, charactergroupid.Value);
        }

        CharacterGroup? characterGroup = await projectRepository.GetGroupAsync(targetGroupId);


        if (characterGroup == null)
        {
            return NotFound();
        }

        return View(new AddCharacterViewModel()
        {
            ProjectId = ProjectId,
            ProjectName = characterGroup.Project.ProjectName,
            ParentCharacterGroupIds = [characterGroup.GetId()],
            ContinueCreating = continueCreating,
            CharacterTypeInfo = CharacterTypeInfo.Default(),
        }.Fill(characterGroup, CurrentUserId, projectInfo));
    }

    [HttpPost("~/{ProjectId}/character/create")]
    [MasterAuthorize(Permission.CanEditRoles)]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(AddCharacterViewModel viewModel)
    {
        var characterGroupId = viewModel.ParentCharacterGroupIds.FirstOrDefault();
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));

        // См. Edit: пустой список групп ловим здесь, а не в сервисе (#5323).
        ValidateCharacterGroups(projectInfo, viewModel);

        // Биндер мог отвергнуть идентификатор группы (чужой проект, нераспознанная строка) — тогда
        // элемент просто не попал в ParentCharacterGroupIds. Создавать роль в молча урезанном
        // наборе групп нельзя: показываем форму с ошибкой привязки.
        if (!ModelState.IsValid)
        {
            return await ShowFormAgain();
        }

        try
        {
            await characterService.AddCharacter(new AddCharacterRequest(
                ProjectId: new(viewModel.ProjectId),
                CharacterTypeInfo: viewModel.CharacterTypeInfo,
                ParentCharacterGroupIds: [.. viewModel.ParentCharacterGroupIds],
                FieldValues: Request.GetFieldsToSetFromPost(projectInfo, FieldValueViewModel.HtmlIdPrefix)
            ));

            if (viewModel.ContinueCreating)
            {
                if (characterGroupId != null)
                {
                    // GET Create принимает номер группы, а не типизированный id — иначе в query
                    // уедет "charactergroupid=CharacterGroupId(5-10)", не разберётся и подставится корень.
                    return RedirectToAction("Create",
                        new { viewModel.ProjectId, characterGroupId = characterGroupId.CharacterGroupId, viewModel.ContinueCreating });
                }
                else
                {
                    return RedirectToAction("Create", new { viewModel.ProjectId, viewModel.ContinueCreating });
                }
            }
            else if (characterGroupId == null)
            {
                return RedirectToAction("Active", "CharacterList", new { viewModel.ProjectId });
            }

            return RedirectToIndex(characterGroupId);
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return await ShowFormAgain();
        }

        async Task<ActionResult> ShowFormAgain()
        {
            CharacterGroup? characterGroup;
            if (characterGroupId == null)
            {
                characterGroup = (await projectRepository.GetProjectAsync(viewModel.ProjectId))
                    .RootGroup;
            }
            else
            {
                characterGroup = await projectRepository.GetGroupAsync(characterGroupId);
                if (characterGroup is null)
                {
                    return NotFound();
                }
            }

            return View(viewModel.Fill(characterGroup, CurrentUserId, projectInfo));
        }
    }

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Delete(ProjectIdentification projectId, int characterId)
    {
        var character = await characterInfoRepository.GetCharacterInfoOrDefault(new CharacterIdentification(projectId, characterId));
        return character is null ? NotFound() : View(new DeleteCharacterViewModel(character));
    }

    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(ProjectIdentification projectId,
        int characterId,
        IFormCollection form)
    {
        var id = new CharacterIdentification(projectId, characterId);
        try
        {
            await characterService.DeleteCharacter(new DeleteCharacterRequest(id));

            return RedirectToAction("Index", "GameGroups", new { ProjectId = projectId.Value, area = "" });
        }
        catch
        {
            return View(new DeleteCharacterViewModel(await characterInfoRepository.GetCharacterInfo(id)));
        }
    }

    private void ValidateCharacterGroups(ProjectInfo projectInfo, CharacterViewModelBase viewModel)
    {
        if (projectInfo.GroupTree.AllowToSetGroups && viewModel.ParentCharacterGroupIds.Length == 0)
        {
            ModelState.AddModelError(
                nameof(viewModel.ParentCharacterGroupIds),
                CharacterViewModelBase.GroupsRequiredErrorMessage);
        }
    }

}
