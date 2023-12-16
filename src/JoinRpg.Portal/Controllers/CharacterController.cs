using JetBrains.Annotations;
using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/character/{characterid}/[action]")]
public class CharacterController : Common.ControllerGameBase
{
    private readonly IProjectMetadataRepository projectMetadataRepository;

    private IPlotRepository PlotRepository { get; }
    private ICharacterRepository CharacterRepository { get; }
    private IUriService UriService { get; }
    private ICharacterService CharacterService { get; }

    public CharacterController(
        IProjectRepository projectRepository,
        IProjectService projectService,
        IPlotRepository plotRepository,
        ICharacterRepository characterRepository,
        IUriService uriService,
        IUserRepository userRepository,
        ICharacterService characterService,
        IProjectMetadataRepository projectMetadataRepository)
        : base(projectRepository, projectService, userRepository)
    {
        PlotRepository = plotRepository;
        CharacterRepository = characterRepository;
        UriService = uriService;
        CharacterService = characterService;
        this.projectMetadataRepository = projectMetadataRepository;
    }

    [HttpGet("~/{projectId}/character/{characterid}/")]
    [HttpGet("~/{projectId}/character/{characterid}/details")]
    [AllowAnonymous]
    public async Task<ActionResult> Details(int projectid, int characterid)
    {
        var field = await CharacterRepository.GetCharacterWithGroups(projectid, characterid);
        return field is null ? NotFound() : await ShowCharacter(field);
    }

    private async Task<ActionResult> ShowCharacter(Character character)
    {
        var plots = character.HasPlotViewAccess(CurrentUserIdOrDefault)
            ? await ShowPlotsForCharacter(character)
            : Enumerable.Empty<PlotElement>();

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(character.ProjectId));
        return View("Details",
            new CharacterDetailsViewModel(CurrentUserIdOrDefault,
                character,
                plots.ToList(),
                UriService, projectInfo));
    }

    private async Task<IReadOnlyList<PlotElement>> ShowPlotsForCharacter(Character character) =>
        character.GetOrderedPlots(await PlotRepository.GetPlotsForCharacter(character));

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Edit(int projectId, int characterId)
    {
        var field = await CharacterRepository.GetCharacterWithDetails(projectId, characterId);
        var view = await CharacterRepository.GetCharacterViewAsync(projectId, characterId);
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
             await CharacterRepository.GetCharacterAsync(viewModel.ProjectId, viewModel.CharacterId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));
        try
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel.Fill(field, CurrentUserId, projectInfo));
            }

            await CharacterService.EditCharacter(
                new EditCharacterRequest(
                    new CharacterIdentification(viewModel.ProjectId, viewModel.CharacterId),
                    ParentCharacterGroupIds: viewModel.ParentCharacterGroupIds,
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
        CharacterGroup characterGroup;
        if (charactergroupid is null)
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
            ParentCharacterGroupIds = new[] { characterGroup.CharacterGroupId },
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
            await CharacterService.AddCharacter(new AddCharacterRequest(
                ProjectId: viewModel.ProjectId,
                CharacterTypeInfo: viewModel.CharacterTypeInfo,
                ParentCharacterGroupIds: viewModel.ParentCharacterGroupIds,
                FieldValues: Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix)
            ));

            if (viewModel.ContinueCreating)
            {
                return RedirectToAction("Create",
                    new { viewModel.ProjectId, characterGroupId, viewModel.ContinueCreating });
            }


            return RedirectToIndex(viewModel.ProjectId, characterGroupId);
        }
        catch (Exception exception)
        {
            ModelState.AddException(exception);
            CharacterGroup characterGroup;
            if (characterGroupId == 0)
            {
                characterGroup = (await ProjectRepository.GetProjectAsync(viewModel.ProjectId))
                    .RootGroup;
            }
            else
            {
                characterGroup =
                    await ProjectRepository.GetGroupAsync(viewModel.ProjectId,
                        characterGroupId);
            }

            var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));

            return View(viewModel.Fill(characterGroup, CurrentUserId, projectInfo));
        }
    }

    [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
    public async Task<ActionResult> Delete(int projectid, int characterid)
    {
        var field = await CharacterRepository.GetCharacterAsync(projectid, characterid);
        if (field == null)
        {
            return NotFound();
        }

        return View(field);
    }

    [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int projectId,
        int characterId,
        [UsedImplicitly]
        IFormCollection form)
    {
        var field = await CharacterRepository.GetCharacterAsync(projectId, characterId);
        try
        {
            await CharacterService.DeleteCharacter(new DeleteCharacterRequest(new CharacterIdentification(projectId, characterId)));

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
            await CharacterService.MoveCharacter(CurrentUserId,
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
