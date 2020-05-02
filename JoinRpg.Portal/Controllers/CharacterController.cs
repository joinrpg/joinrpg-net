using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JetBrains.Annotations;
using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Controllers
{
    [Route("{projectId}/character/{characterid}/[action]")]
    public class CharacterController : Common.ControllerGameBase
    {
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
            ICharacterService characterService)
            : base(projectRepository, projectService, userRepository)
        {
            PlotRepository = plotRepository;
            CharacterRepository = characterRepository;
            UriService = uriService;
            CharacterService = characterService;
        }

        [HttpGet]
        public async Task<ActionResult> Details(int projectid, int characterid)
        {
            var field = await CharacterRepository.GetCharacterWithGroups(projectid, characterid);
            return (field?.Project == null ? NotFound() : null) ?? await ShowCharacter(field);
        }

        private async Task<ActionResult> ShowCharacter(Character character)
        {
            var plots = character.HasPlotViewAccess(CurrentUserIdOrDefault)
                ? await ShowPlotsForCharacter(character)
                : Enumerable.Empty<PlotElement>();
            return View("Details",
                new CharacterDetailsViewModel(CurrentUserIdOrDefault,
                    character,
                    plots.ToList(),
                    UriService));
        }

        private async Task<IReadOnlyList<PlotElement>> ShowPlotsForCharacter(Character character) =>
            character.GetOrderedPlots(await PlotRepository.GetPlotsForCharacter(character));

        [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> Edit(int projectId, int characterId)
        {
            var field = await CharacterRepository.GetCharacterWithDetails(projectId, characterId);
            return View(new EditCharacterViewModel()
            {
                ProjectId = field.ProjectId,
                CharacterId = field.CharacterId,
                IsPublic = field.IsPublic,
                ProjectName = field.Project.ProjectName,
                IsAcceptingClaims = field.IsAcceptingClaims,
                HidePlayerForCharacter = field.HidePlayerForCharacter,
                Name = field.CharacterName,
                ParentCharacterGroupIds = field.GetParentGroupsForEdit(),
                IsHot = field.IsHot,
            }.Fill(field, CurrentUserId));
        }

        [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditCharacterViewModel viewModel)
        {
            var field =
                await CharacterRepository.GetCharacterAsync(viewModel.ProjectId,
                    viewModel.CharacterId);
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(viewModel.Fill(field, CurrentUserId));
                }

                await CharacterService.EditCharacter(
                    CurrentUserId,
                    viewModel.CharacterId,
                    viewModel.ProjectId,
                    viewModel.Name,
                    viewModel.IsPublic,
                    viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(),
                    viewModel.IsAcceptingClaims &&
                    field.ApprovedClaim == null,
                    viewModel.HidePlayerForCharacter,
                    Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix),
                    viewModel.IsHot);

                return RedirectToAction("Details",
                    new {viewModel.ProjectId, viewModel.CharacterId});
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                return View(viewModel.Fill(field, CurrentUserId));
            }
        }

        [HttpGet("~/{ProjectId}/character/create")]
        [MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> Create(int projectid, int charactergroupid, bool continueCreating = false)
        {
            var characterGroup = await ProjectRepository.GetGroupAsync(projectid, charactergroupid);

            if (characterGroup == null) return NotFound();

            return View(new AddCharacterViewModel()
            {
                ProjectId = projectid,
                ProjectName = characterGroup.Project.ProjectName,
                ParentCharacterGroupIds = characterGroup.AsPossibleParentForEdit(),
                ContinueCreating = continueCreating,
            }.Fill(characterGroup, CurrentUserId));
        }

        [HttpPost("~/{ProjectId}/character/create")]
        [MasterAuthorize(Permission.CanEditRoles)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AddCharacterViewModel viewModel)
        {
            var characterGroupId = viewModel.ParentCharacterGroupIds.GetUnprefixedGroups().FirstOrDefault();
            try
            {
                await CharacterService.AddCharacter(new AddCharacterRequest()
                {
                    ProjectId = viewModel.ProjectId,
                    Name = viewModel.Name,
                    IsAcceptingClaims = viewModel.IsAcceptingClaims,
                    ParentCharacterGroupIds =
                        viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(),
                    HidePlayerForCharacter = viewModel.HidePlayerForCharacter,
                    IsHot = viewModel.IsHot,
                    IsPublic = viewModel.IsPublic,
                    FieldValues = Request.GetDynamicValuesFromPost(FieldValueViewModel.HtmlIdPrefix),
                });



                if (viewModel.ContinueCreating)
                {
                    return RedirectToAction("Create",
                        new {viewModel.ProjectId, characterGroupId, viewModel.ContinueCreating});
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

                return View(viewModel.Fill(characterGroup, CurrentUserId));
            }
        }

        [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> Delete(int projectid, int characterid)
        {
            var field = await CharacterRepository.GetCharacterAsync(projectid, characterid);
            if (field == null) return NotFound();
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
                await CharacterService.DeleteCharacter(projectId, characterId, CurrentUserId);

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
}
