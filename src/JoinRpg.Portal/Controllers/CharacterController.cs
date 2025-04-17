using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.WebPortal.Managers.Plots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/character/{characterid}/[action]")]
public class CharacterController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    ICharacterRepository characterRepository,
    IUriService uriService,
    IUserRepository userRepository,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUser,
    CharacterPlotViewService characterPlotViewService
        ) : Common.ControllerGameBase(projectRepository, projectService, userRepository)
{

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
        var plots = await characterPlotViewService.GetPlotsForCharacter(character.GetId());

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(character.ProjectId));
        return View("Details",
            new CharacterDetailsViewModel(currentUser,
                character,
                plots,
                uriService, projectInfo));
    }

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Edit(int projectId, int characterId)
    {
        var field = await characterRepository.GetCharacterWithDetails(projectId, characterId);
        var view = await characterRepository.GetCharacterViewAsync(projectId, characterId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectId));
        return View(new EditCharacterViewModel()
        {
            ProjectId = field.ProjectId,
            CharacterId = field.CharacterId,
            ProjectName = field.Project.ProjectName,
            CharacterTypeInfo = view.CharacterTypeInfo,
            Name = field.CharacterName,
            ParentCharacterGroupIds = field.Groups.Where(gr => !gr.IsSpecial).Select(pg => pg.CharacterGroupId).ToArray(),
        }.Fill(field, CurrentUserId, projectInfo));
    }

    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditCharacterViewModel viewModel)
    {
        var field =
             await characterRepository.GetCharacterAsync(viewModel.ProjectId, viewModel.CharacterId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));
        try
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel.Fill(field, CurrentUserId, projectInfo));
            }

            await characterService.EditCharacter(
                new EditCharacterRequest(
                    new CharacterIdentification(viewModel.ProjectId, viewModel.CharacterId),
                    ParentCharacterGroupIds: [.. CharacterGroupIdentification.FromList(viewModel.ParentCharacterGroupIds, new(viewModel.ProjectId))],
                    CharacterTypeInfo: viewModel.CharacterTypeInfo,
                    FieldValues: Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix))
                );

            return RedirectToAction("Details",
                new { viewModel.ProjectId, viewModel.CharacterId });
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            return View(viewModel.Fill(field, CurrentUserId, projectInfo));
        }
    }

    [HttpGet("~/{ProjectId}/character/create")]
    [MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Create(int projectid, int? charactergroupid, bool continueCreating = false)
    {
        CharacterGroup? characterGroup;
        if (charactergroupid is null || charactergroupid == 0)
        {
            characterGroup = await ProjectRepository.GetRootGroupAsync(projectid);
        }
        else
        {
            characterGroup = await ProjectRepository.GetGroupAsync(projectid, charactergroupid.Value);
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectid));

        if (characterGroup == null)
        {
            return NotFound();
        }

        return View(new AddCharacterViewModel()
        {
            ProjectId = projectid,
            ProjectName = characterGroup.Project.ProjectName,
            ParentCharacterGroupIds = [characterGroup.CharacterGroupId],
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
        try
        {
            await characterService.AddCharacter(new AddCharacterRequest(
                ProjectId: new(viewModel.ProjectId),
                CharacterTypeInfo: viewModel.CharacterTypeInfo,
                ParentCharacterGroupIds: [.. CharacterGroupIdentification.FromList(viewModel.ParentCharacterGroupIds, new(viewModel.ProjectId))],
                FieldValues: Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix)
            ));

            if (viewModel.ContinueCreating)
            {
                if (characterGroupId != 0)
                {
                    return RedirectToAction("Create",
                        new { viewModel.ProjectId, characterGroupId, viewModel.ContinueCreating });
                }
                else
                {
                    return RedirectToAction("Create", new { viewModel.ProjectId, viewModel.ContinueCreating });
                }
            }
            else if (characterGroupId == 0)
            {
                return RedirectToAction("Active", "CharacterList", new { viewModel.ProjectId });
            }

            return RedirectToIndex(viewModel.ProjectId, characterGroupId);
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            CharacterGroup? characterGroup;
            if (characterGroupId == 0)
            {
                characterGroup = (await ProjectRepository.GetProjectAsync(viewModel.ProjectId))
                    .RootGroup;
            }
            else
            {
                characterGroup = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, characterGroupId);
                if (characterGroup is null)
                {
                    return NotFound();
                }
            }

            var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));

            return View(viewModel.Fill(characterGroup, CurrentUserId, projectInfo));
        }
    }

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Delete(int projectid, int characterid)
    {
        var field = await characterRepository.GetCharacterAsync(projectid, characterid);
        if (field == null)
        {
            return NotFound();
        }

        return View(field);
    }

    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int projectId,
        int characterId,
        IFormCollection form)
    {
        var field = await characterRepository.GetCharacterAsync(projectId, characterId);
        try
        {
            await characterService.DeleteCharacter(new DeleteCharacterRequest(new CharacterIdentification(projectId, characterId)));

            return RedirectToIndex(field.Project);
        }
        catch
        {
            return View(field);
        }
    }

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public Task<ActionResult> MoveUp(int projectid,
        int characterid,
        int parentcharactergroupid,
        int currentrootgroupid) => MoveImpl(projectid,
        characterid,
        parentcharactergroupid,
        currentrootgroupid,
        direction: -1);

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public Task<ActionResult> MoveDown(int projectid,
        int characterid,
        int parentcharactergroupid,
        int currentrootgroupid) => MoveImpl(projectid,
        characterid,
        parentcharactergroupid,
        currentrootgroupid,
        direction: +1);

    private async Task<ActionResult> MoveImpl(int projectId,
        int characterId,
        int parentCharacterGroupId,
        int currentRootGroupId,
        short direction)
    {
        try
        {
            await characterService.MoveCharacter(CurrentUserId,
                projectId,
                characterId,
                parentCharacterGroupId,
                direction);

            return RedirectToIndex(projectId, currentRootGroupId);
        }
        catch
        {
            //TODO Show Error
            return RedirectToIndex(projectId, currentRootGroupId);
        }
    }
}
