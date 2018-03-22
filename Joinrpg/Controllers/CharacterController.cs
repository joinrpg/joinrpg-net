using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Controllers
{
    public class CharacterController : Common.ControllerGameBase
    {
        private IPlotRepository PlotRepository { get; }
        private ICharacterRepository CharacterRepository { get; }
        private IUriService UriService { get; }

        public CharacterController(ApplicationUserManager userManager,
            IProjectRepository projectRepository,
            IProjectService projectService,
            IPlotRepository plotRepository,
            IExportDataService exportDataService,
            ICharacterRepository characterRepository,
            IUriService uriService)
            : base(userManager, projectRepository, projectService, exportDataService)
        {
            PlotRepository = plotRepository;
            CharacterRepository = characterRepository;
            UriService = uriService;
        }

        [HttpGet]
        public async Task<ActionResult> Details(int projectid, int characterid)
        {
            var field = await CharacterRepository.GetCharacterWithGroups(projectid, characterid);
            return (field?.Project == null ? HttpNotFound() : null) ?? await ShowCharacter(field);
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
                Description = field.Description.Contents,
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

                await ProjectService.EditCharacter(
                    CurrentUserId,
                    viewModel.CharacterId,
                    viewModel.ProjectId,
                    viewModel.Name,
                    viewModel.IsPublic,
                    viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(),
                    viewModel.IsAcceptingClaims &&
                    field.ApprovedClaim == null, //Force this field to false if has approved claim
                    viewModel.Description,
                    viewModel.HidePlayerForCharacter,
                    GetCustomFieldValuesFromPost(),
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

        [HttpGet]
        [MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> Create(int projectid, int charactergroupid, bool continueCreating = false)
        {
            var characterGroup = await ProjectRepository.GetGroupAsync(projectid, charactergroupid);

            if (characterGroup == null) return HttpNotFound();

            return View(new AddCharacterViewModel()
            {
                ProjectId = projectid,
                ProjectName = characterGroup.Project.ProjectName,
                ParentCharacterGroupIds = characterGroup.AsPossibleParentForEdit(),
                ContinueCreating = continueCreating,
            }.Fill(characterGroup, CurrentUserId));
        }

        [HttpPost]
        [MasterAuthorize(Permission.CanEditRoles)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AddCharacterViewModel viewModel)
        {
            try
            {
                await ProjectService.AddCharacter(new AddCharacterRequest()
                {
                    ProjectId = viewModel.ProjectId,
                    Description = viewModel.Description,
                    Name = viewModel.Name,
                    IsAcceptingClaims = viewModel.IsAcceptingClaims,
                    ParentCharacterGroupIds =
                        viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(),
                    HidePlayerForCharacter = viewModel.HidePlayerForCharacter,
                    IsHot = viewModel.IsHot,
                    IsPublic = viewModel.IsPublic,
                    FieldValues = GetCustomFieldValuesFromPost(),
                });

                var characterGroupId = viewModel.ParentCharacterGroupIds.GetUnprefixedGroups().First();

                if (viewModel.ContinueCreating)
                {
                    return RedirectToAction("Create",
                        new {viewModel.ProjectId, characterGroupId, viewModel.ContinueCreating});
                }

                
                return RedirectToIndex(viewModel.ProjectId, characterGroupId);
            }
            catch
            {
                return View(viewModel);
            }
        }

        [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> Delete(int projectid, int characterid)
        {
            var field = await CharacterRepository.GetCharacterAsync(projectid, characterid);
            if (field == null) return HttpNotFound();
            return View(field);
        }

        [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int projectId,
            int characterId,
            [UsedImplicitly]
            FormCollection form)
        {
            var field = await CharacterRepository.GetCharacterAsync(projectId, characterId);
            try
            {
                await ProjectService.DeleteCharacter(projectId, characterId, CurrentUserId);

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
                await ProjectService.MoveCharacter(CurrentUserId,
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
