using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Subscribe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Route("{projectId}/roles/{characterGroupId}/[action]")]
    public class GameGroupsController : ControllerGameBase
    {

        public GameGroupsController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IUserRepository userRepository)
            : base(projectRepository, projectService, userRepository)
        {
        }

        [HttpGet("~/{projectId}/roles/{characterGroupId?}")]
        [AllowAnonymous]
        public async Task<ActionResult> Index(int projectId, int? characterGroupId)
        {
            var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

            if (field == null)
            {
                return NotFound();
            }

            return View(
              new GameRolesViewModel
              {
                  ProjectId = field.Project.ProjectId,
                  ProjectName = field.Project.ProjectName,
                  ShowEditControls = field.HasEditRolesAccess(CurrentUserIdOrDefault),
                  HasMasterAccess = field.HasMasterAccess(CurrentUserIdOrDefault),
                  Data = CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault),
                  Details = new CharacterGroupDetailsViewModel(field, CurrentUserIdOrDefault, GroupNavigationPage.Roles),
              });
        }

        [MasterAuthorize]
        [HttpGet("~/{projectId}/roles/report")]
        public Task<ActionResult> Report(int projectId) => Report(projectId, null);

        [HttpGet, MasterAuthorize]
        public async Task<ActionResult> Report(int projectId, int? characterGroupId)
        {
            var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

            if (field == null)
            {
                return NotFound();
            }

            return View(
              new GameRolesReportViewModel
              {
                  ProjectId = field.Project.ProjectId,
                  Data = CharacterGroupReportViewModel.GetGroups(field),
                  Details = new CharacterGroupDetailsViewModel(field, CurrentUserIdOrDefault, GroupNavigationPage.Report),
                  CheckinModuleEnabled = field.Project.Details.EnableCheckInModule,
              });
        }

        [HttpGet("~/{projectId}/roles/hot")]
        [AllowAnonymous]
        public Task<ActionResult> Hot(int projectId) => Hot(projectId, null);

        [HttpGet]
        public async Task<ActionResult> Hot(int projectId, int? characterGroupId)
        {
            var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
            if (field == null)
            {
                return NotFound();
            }

            return View(GetHotCharacters(field));
        }

        [HttpGet]
        [HttpGet("~/{projectId}/roles/hotjson")]
        public async Task<ActionResult> HotJson(int projectId, int characterGroupId, int? maxCount = null)
        {
            var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
            if (field == null)
            {
                return NotFound();
            }

            var hotRoles = GetHotCharacters(field).Shuffle().Take(maxCount ?? int.MaxValue);

            return ReturnJson(hotRoles.Select(ConvertCharacterToJson));
        }

        private IEnumerable<CharacterViewModel> GetHotCharacters(CharacterGroup field)
        {
            return CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault)
              .SelectMany(
                g => g.PublicCharacters.Where(ch => ch.IsHot && ch.IsFirstCopy)).Distinct();
        }

        [HttpGet("~/{projectId}/roles/{characterGroupId}/indexjson")]
        [AllowAnonymous]
        public async Task<ActionResult> IndexJson(int projectId, int characterGroupId)
        {
            var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
            if (field == null)
            {
                return NotFound();

            }

            var hasMasterAccess = field.HasMasterAccess(CurrentUserIdOrDefault);
            return ReturnJson(new
            {
                field.Project.ProjectId,
                field.Project.ProjectName,
                ShowEditControls = hasMasterAccess,
                Groups = CharacterGroupListViewModel.GetGroups(field, CurrentUserIdOrDefault).Select(
                    g =>
                      new
                      {
                          g.CharacterGroupId,
                          g.Name,
                          g.DeepLevel,
                          g.FirstCopy,
                          Description = g.Description?.ToHtmlString(),
                          Path = g.Path.Select(gr => gr.Name),
                          PathIds = g.Path.Select(gr => gr.CharacterGroupId),
                          Characters = g.PublicCharacters.Select(ConvertCharacterToJson),
                          CanAddDirectClaim = g.IsAcceptingClaims,
                          DirectClaimsCount = g.AvaiableDirectSlots,
                          DirectClaimLink = g.IsAcceptingClaims ? GetFullyQualifiedUri("AddForGroup", "Claim", new { field.ProjectId, g.CharacterGroupId }) : null,
                      }),
            });
        }

        [HttpGet("~/{projectId}/roles/json_full")]
        public Task<ActionResult> Json_Full(int projectId) => AllGroupsJson(projectId, includeSpecial: true);

        [HttpGet("~/{projectId}/roles/json_real")]
        public Task<ActionResult> Json_Real(int projectId) => AllGroupsJson(projectId, includeSpecial: false);

        private async Task<ActionResult> AllGroupsJson(int projectId, bool includeSpecial)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            var cached = CheckCache(project.CharacterTreeModifiedAt);
            if (cached)
            {
                return NotModified();
            }

            var field = await ProjectRepository.LoadGroupWithTreeSlimAsync(projectId);
            if (field == null)
            {
                return NotFound();
            }
            return ReturnJson(new
            {
                field.Project.ProjectId,
                Groups =
                new CharacterTreeBuilder(field, CurrentUserId).Generate()
                  .Where(g => includeSpecial || !g.IsSpecial)
                  .Select(
                    g =>
                      new
                      {
                          g.CharacterGroupId,
                          g.Name,
                          g.DeepLevel,
                          g.FirstCopy,
                          Path = g.Path.Select(gr => gr.Name),
                          PathIds = g.Path.Select(gr => gr.CharacterGroupId),
                          Characters = g.Characters.Select(ConvertCharacterToJsonSlim),
                      }),
            });
        }

        private JsonResult ReturnJson(object data)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            return Json(data, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = null, // Pascal case
            });
        }

        private object ConvertCharacterToJson(CharacterViewModel ch)
        {
            return new
            {
                ch.CharacterId, //TODO Remove
                CharacterLink = GetFullyQualifiedUri("Details", "Character", new { ch.CharacterId }),
                ch.IsAvailable,
                ch.IsFirstCopy,
                ch.CharacterName,
                Description = ch.Description?.ToHtmlString(),
                PlayerName = ch.HidePlayer ? "скрыто" : ch.Player?.GetDisplayName(),
                PlayerId = ch.HidePlayer ? null : ch.Player?.UserId, //TODO Remove
                PlayerLink = (ch.HidePlayer || ch.Player == null) ? null : GetFullyQualifiedUri("Details", "User", new { ch.Player?.UserId }),
                ch.ActiveClaimsCount,
                ClaimLink =
                ch.IsAvailable
                  ? GetFullyQualifiedUri("AddForCharacter", "Claim", new { ch.ProjectId, ch.CharacterId })
                  : null,
            };
        }

        private object ConvertCharacterToJsonSlim(CharacterLinkViewModel ch)
        {
            return new
            {
                ch.CharacterId,
                ch.IsAvailable,
                ch.IsFirstCopy,
                ch.CharacterName,
            };
        }

        [HttpGet, Authorize]
        [MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> Edit(int projectId, int characterGroupId)
        {
            var group = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);

            if (group == null)
            {
                return NotFound();
            }

            if (group.IsSpecial)
            {
                return Content("Can't edit special group");
            }

            return View(FillFromCharacterGroup(new EditCharacterGroupViewModel
            {
                ParentCharacterGroupIds = group.GetParentGroupsForEdit(),
                Description = group.Description.Contents,
                IsPublic = group.IsPublic,
                Name = group.CharacterGroupName,
                HaveDirectSlots = GetDirectClaimSettings(group),
                DirectSlots = Math.Max(group.AvaiableDirectSlots, 0),
                CharacterGroupId = group.CharacterGroupId,
                IsRoot = group.IsRoot,
                ResponsibleMasterId = group.ResponsibleMasterUserId ?? -1,
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt,
                CreatedBy = group.CreatedBy,
                UpdatedBy = group.UpdatedBy,
            }, group));
        }

        private static IEnumerable<MasterListItemViewModel> GetMasters(IClaimSource group, bool includeSelf)
        {
            return group.Project.GetMasterListViewModel()
              .Union(new MasterListItemViewModel()
              {
                  Id = "-1",
                  Name = "По умолчанию", // TODO Temporary disabled as shown in hot profiles + GetDefaultResponsible(group, includeSelf)
              }).OrderByDescending(m => m.Id == "-1").ThenBy(m => m.Name);
        }

        private static string GetDefaultResponsible(IClaimSource group, bool includeSelf)
        {
            var result = ResponsibleMasterExtensions.GetResponsibleMasters(@group, includeSelf)
                .Select(u => u.GetDisplayName())
                .JoinStrings(", ");
            return string.IsNullOrWhiteSpace(result) ? "Никто" : result;
        }

        private static DirectClaimSettings GetDirectClaimSettings(CharacterGroup group)

        {
            if (!group.HaveDirectSlots)
            {
                return DirectClaimSettings.NoDirectClaims;
            }

            return group.DirectSlotsUnlimited ? DirectClaimSettings.DirectClaimsUnlimited : DirectClaimSettings.DirectClaimsLimited;
        }

        [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> Edit(EditCharacterGroupViewModel viewModel)
        {
            var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);
            if (group == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid && !group.IsRoot) //TODO: We can't actually validate root group — too many errors.
            {
                viewModel.IsRoot = group.IsRoot;
                return View(FillFromCharacterGroup(viewModel, group));
            }

            if (group.IsSpecial)
            {
                return Content("Can't edit special group");
            }

            try
            {
                var responsibleMasterId = viewModel.ResponsibleMasterId == -1 ? (int?)null : viewModel.ResponsibleMasterId;
                await ProjectService.EditCharacterGroup(
                  group.ProjectId,
                  CurrentUserId,
                  group.CharacterGroupId, viewModel.Name, viewModel.IsPublic,
                  viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(), viewModel.Description, viewModel.HaveDirectSlotsForSave(),
                  viewModel.DirectSlotsForSave(), responsibleMasterId);

                return RedirectToIndex(group.ProjectId, group.CharacterGroupId, "Details");
            }
            catch (Exception e)
            {
                ModelState.AddException(e);
                viewModel.IsRoot = group.IsRoot;
                return View(FillFromCharacterGroup(viewModel, group));

            }

        }

        [HttpGet, MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> Delete(int projectId, int characterGroupId)
        {
            var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

            if (field == null)
            {
                return NotFound();
            }

            return View(field);
        }


        [HttpPost, MasterAuthorize(Permission.CanEditRoles), ValidateAntiForgeryToken]
        // ReSharper disable once UnusedParameter.Global
        public async Task<ActionResult> Delete(int projectId, int characterGroupId, IFormCollection collection)
        {
            var field = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

            if (field == null)
            {
                return NotFound();
            }

            var project = field.Project;
            try
            {
                await ProjectService.DeleteCharacterGroup(projectId, characterGroupId);

                return RedirectToIndex(project);
            }
            catch
            {
                return View(field);
            }
        }

        [HttpGet]
        [MasterAuthorize(Permission.CanEditRoles)]
        public async Task<ActionResult> AddGroup(int projectid, int charactergroupid)
        {
            var field = await ProjectRepository.GetGroupAsync(projectid, charactergroupid);

            if (field == null)
            {
                return NotFound();
            }

            return View(FillFromCharacterGroup(new AddCharacterGroupViewModel()
            {
                ParentCharacterGroupIds = field.AsPossibleParentForEdit(),
                ResponsibleMasterId = -1,
            }, field));
        }

        [HttpPost]
        [MasterAuthorize(Permission.CanEditRoles)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddGroup(AddCharacterGroupViewModel viewModel, int charactergroupid)
        {
            var field = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, charactergroupid);
            if (field == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(FillFromCharacterGroup(viewModel, field));
            }

            try
            {
                var responsibleMasterId = viewModel.ResponsibleMasterId == -1 ? (int?)null : viewModel.ResponsibleMasterId;
                await ProjectService.AddCharacterGroup(
                  viewModel.ProjectId,
                  viewModel.Name, viewModel.IsPublic,
                  viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(), viewModel.Description, viewModel.HaveDirectSlotsForSave(),
                  viewModel.DirectSlotsForSave(), responsibleMasterId);

                return RedirectToIndex(field.ProjectId, viewModel.ParentCharacterGroupIds.GetUnprefixedGroups().First());
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                return View(FillFromCharacterGroup(viewModel, field));
            }
        }

        private static T FillFromCharacterGroup<T>(T viewModel, IClaimSource field)
          where T : CharacterGroupViewModelBase
        {
            viewModel.Masters = GetMasters(field, includeSelf: true);
            viewModel.ProjectName = field.Project.ProjectName;
            viewModel.ProjectId = field.Project.ProjectId;
            return viewModel;
        }

        [MasterAuthorize(Permission.CanEditRoles)]
        [HttpPost]
        public Task<ActionResult> MoveUp(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId) => MoveImpl(projectId, charactergroupId, parentCharacterGroupId, currentRootGroupId, -1);

        private async Task<ActionResult> MoveImpl(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId, short direction)
        {

            try
            {
                await ProjectService.MoveCharacterGroup(CurrentUserId, projectId, charactergroupId, parentCharacterGroupId, direction);


                return RedirectToIndex(projectId, currentRootGroupId);
            }
            catch
            {
                return RedirectToIndex(projectId, currentRootGroupId);
            }
        }

        [MasterAuthorize(Permission.CanEditRoles)]
        [HttpPost]
        public Task<ActionResult> MoveDown(int projectId, int charactergroupId, int parentCharacterGroupId, int currentRootGroupId) => MoveImpl(projectId, charactergroupId, parentCharacterGroupId, currentRootGroupId, +1);

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> EditSubscribe(int projectId, int characterGroupId)
        {
            var group = await ProjectRepository.LoadGroupWithTreeAsync(projectId, characterGroupId);
            if (group == null)
            {
                return NotFound();
            }

            var user = await UserRepository.GetWithSubscribe(CurrentUserId);

            return View(new SubscribeSettingsViewModel(user, group));
        }

        [HttpPost, ValidateAntiForgeryToken, MasterAuthorize()]
        public async Task<ActionResult> EditSubscribe(SubscribeSettingsViewModel viewModel)
        {
            var group = await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);

            if (group == null)
            {
                return NotFound();
            }

            var user = await UserRepository.GetWithSubscribe(CurrentUserId);

            var serverModel = new SubscribeSettingsViewModel(user, group);

            _ = serverModel.Options.AssignFrom(viewModel.Options);

            try
            {
                await
                    ProjectService.UpdateSubscribeForGroup(new SubscribeForGroupRequest
                    {
                        CharacterGroupId = group.CharacterGroupId,
                        ProjectId = group.ProjectId,
                    }.AssignFrom(serverModel.GetOptionsToSubscribeDirectly()));

                return RedirectToIndex(group.Project);
            }
            catch (Exception e)
            {
                ModelState.AddException(e);
                return View(serverModel);
            }

        }

        [HttpGet, AllowAnonymous]
        public async Task<ActionResult> Details(int projectId, int characterGroupId)
        {
            var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
            if (group == null)
            {
                return NotFound();
            }
            var viewModel = new CharacterGroupDetailsViewModel(group, CurrentUserIdOrDefault, GroupNavigationPage.Home);
            return View(viewModel);
        }

        protected string GetFullyQualifiedUri([AspMvcAction]
        string actionName,
            [AspMvcController]
        string controllerName,
            object routeValues)
        {
            var url = new Uri(Request.GetDisplayUrl());
            if (url == null)
            {
                throw new InvalidOperationException("Request.Url is unexpectedly null");
            }

            return url.Scheme + "://" + url.Host +
                   (url.IsDefaultPort ? "" : $":{url.Port}") +
                   Url.Action(actionName, controllerName, routeValues);
        }

        private bool IsClientCached(DateTime contentModified)
        {
            string header = Request.Headers["If-Modified-Since"];

            if (header == null)
            {
                return false;
            }

            return DateTime.TryParse(header, out var isModifiedSince) &&
                   isModifiedSince.ToUniversalTime() > contentModified;
        }

        protected bool CheckCache(DateTime characterTreeModifiedAt)
        {
            if (IsClientCached(characterTreeModifiedAt))
            {
                return true;
            }

            Response.Headers.Add("Last-Modified", characterTreeModifiedAt.ToString("R"));
            return false;
        }

    }


}
