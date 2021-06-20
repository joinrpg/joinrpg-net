using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Route("{ProjectId}/claims/[action]")]
    public class ClaimListController : Common.ControllerGameBase
    {
        private IExportDataService ExportDataService { get; }
        private IClaimsRepository ClaimsRepository { get; }
        private IAccommodationRepository AccommodationRepository { get; }
        private IUriService UriService { get; }

        public ClaimListController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IExportDataService exportDataService,
            IClaimsRepository claimsRepository,
            IUriService uriService,
            IAccommodationRepository accommodationRepository,
            IUserRepository userRepository)
            : base(projectRepository, projectService, userRepository)
        {
            ExportDataService = exportDataService;
            ClaimsRepository = claimsRepository;
            UriService = uriService;
            AccommodationRepository = accommodationRepository;
        }

        #region implementation

        private async Task<ActionResult> ShowMasterClaimList(int projectId, string export, string title,
            [AspMvcView] string viewName, IReadOnlyCollection<Claim> claims)
        {
            var view = new ClaimListViewModel(CurrentUserId, claims, projectId);

            var exportType = ExportTypeNameParserHelper.ToExportType(export);

            if (exportType == null)
            {
                ViewBag.MasterAccessColumn = true;
                ViewBag.Title = title;
                return View(viewName, view);
            }
            else
            {
                var project = await GetProjectFromList(projectId, claims);

                return
                    await
                        ExportWithCustomFrontend(view.Items, title, exportType.Value,
                            new ClaimListItemViewModelExporter(project, UriService), project.ProjectName);
            }
        }

        private async Task<ActionResult> ShowMasterClaimListForGroup(CharacterGroup characterGroup, string export,
            string title, IReadOnlyCollection<Claim> claims, GroupNavigationPage page)
        {
            if (characterGroup == null)
            {
                return NotFound();
            }

            var view = new ClaimListForGroupViewModel(CurrentUserId, claims, characterGroup, page);

            var exportType = ExportTypeNameParserHelper.ToExportType(export);

            if (exportType == null)
            {
                ViewBag.MasterAccessColumn = true;
                ViewBag.Title = title + " " + characterGroup.CharacterGroupName;
                return View("ByGroup", view);
            }
            else
            {
                return
                    await
                        ExportWithCustomFrontend(view.Items, title, exportType.Value,
                            new ClaimListItemViewModelExporter(characterGroup.Project, UriService),
                            characterGroup.Project.ProjectName);
            }
        }

        #endregion

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> ForPlayer(int projectId, int userId, string export)
        {
            var claims = await ClaimsRepository.GetClaimsForPlayer(projectId, ClaimStatusSpec.Active, userId);

            return await ShowMasterClaimList(projectId, export, "Заявки на игроке", "Index", claims);
        }

        [HttpGet("~/{ProjectId}/claims/without-roomtype")]
        [MasterAuthorize()]
        public Task<ActionResult> ListWithoutRoomType(int projectId, string export) =>
            ListForRoomType(projectId, null, export);

        [HttpGet("~/{ProjectId}/claims/by-roomtype/{roomTypeId?}")]
        [MasterAuthorize()]
        public async Task<ActionResult> ListForRoomType(int projectId, int? roomTypeId, string export)
        {
            var claims = await ClaimsRepository.GetClaimsForRoomType(projectId, ClaimStatusSpec.Active, roomTypeId);

            string title;
            if (roomTypeId == null)
            {
                title = "Активные заявки без поселения";
            }
            else
            {
                title = "Активные заявки по типу поселения: " +
                        (await AccommodationRepository.GetRoomTypeById(roomTypeId.Value)).Name;
            }

            return await ShowMasterClaimList(projectId, export, title, "Index", claims);
        }

        [HttpGet("~/{ProjectId}/roles/{CharacterGroupId}/discussing")]
        [MasterAuthorize()]
        public async Task<ActionResult> ListForGroupDirect(int projectId, int characterGroupId, string export)
        {
            var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
            var claims =
                await ClaimsRepository.GetClaimsForGroupDirect(projectId, ClaimStatusSpec.Active, characterGroupId);

            return await ShowMasterClaimListForGroup(group, export, "Обсуждаемые заявки (в группу)", claims,
                GroupNavigationPage.ClaimsDirect);
        }

        [HttpGet("~/{ProjectId}/roles/{CharacterGroupId}/claims")]
        [MasterAuthorize()]
        public async Task<ActionResult> ListForGroup(int projectId, int characterGroupId, string export)
        {
            var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
            var groupIds = await GetChildrenGroupIds(projectId, characterGroupId);
            var claims = await ClaimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Active, groupIds);

            return await ShowMasterClaimListForGroup(group, export, "Заявки в группу (все)", claims,
                GroupNavigationPage.ClaimsActive);
        }

        private async Task<int[]> GetChildrenGroupIds(int projectId, int characterGroupId)
        {
            var groups = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
            return groups.GetChildrenGroups().Select(g => g.CharacterGroupId).Union(characterGroupId).ToArray();
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> DiscussingForGroup(int projectId, int characterGroupId, string export)
        {
            var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
            var groupIds = await GetChildrenGroupIds(projectId, characterGroupId);
            var claims = await ClaimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Discussion, groupIds);

            return await ShowMasterClaimListForGroup(group, export, "Обсуждаемые заявки в группу (все)",
                claims, GroupNavigationPage.ClaimsDiscussing);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> ResponsibleDiscussing(int projectid, int responsibleMasterId, string export)
        {
            var claims = await ClaimsRepository.GetClaimsForMaster(projectid, responsibleMasterId,
                ClaimStatusSpec.Discussion);

            return await ShowMasterClaimList(projectid, export, "Обсуждаемые заявки на мастере", "Index", claims);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> ResponsibleOnHold(int projectid, int responsiblemasterid, string export)
        {
            var claims = await ClaimsRepository.GetClaimsForMaster(projectid, responsiblemasterid,
                ClaimStatusSpec.OnHold);

            return await ShowMasterClaimList(projectid, export, "Лист ожидания на мастере", "Index", claims);
        }


        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> ActiveList(int projectId, string export)
        {
            var claims = await ClaimsRepository.GetClaims(projectId, ClaimStatusSpec.Active);

            return await ShowMasterClaimList(projectId, export, "Активные заявки", "Index", claims);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> DeclinedList(int projectId, string export)
        {
            var claims = (await ClaimsRepository.GetClaims(projectId, ClaimStatusSpec.InActive)).ToList();

            return await ShowMasterClaimList(projectId, export, "Отклоненные/отозванные заявки", "Index", claims);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> Discussing(int projectId, string export)
        {
            var claims = (await ClaimsRepository.GetClaims(projectId, ClaimStatusSpec.Discussion)).ToList();

            return await ShowMasterClaimList(projectId, export, "Обсуждаемые заявки", "Index", claims);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> OnHoldList(int projectid, string export)
        {
            var claims = (await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.OnHold)).ToList();

            return await ShowMasterClaimList(projectid, export, "Лист ожидания", "Index", claims);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> WaitingForFee(int projectid, string export)
        {
            var claims =
                (await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved))
                .Where(claim => !claim.ClaimPaidInFull())
                .ToList();

            return await ShowMasterClaimList(projectid, export, "Неоплаченные принятые заявки", "Index", claims);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> SomeFieldsToFill(int projectid, string export)
        {
            var project = await ProjectRepository.GetProjectAsync(projectid);
            var claims =
                (await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.Approved)).Where(
                    claim => claim.HasProblemsForFields(project.ProjectFields.Where(p => p.CanPlayerEdit))).ToList();

            return await ShowMasterClaimList(projectid, export, "Заявки с незаполненными полями", "Index", claims);
        }

        [HttpGet("~/{ProjectId}/claims/for-master/{ResponsibleMasterId}")]
        [MasterAuthorize()]

        public async Task<ActionResult> Responsible(int projectid, int responsibleMasterId, string export)
        {
            var claims =
                (await ClaimsRepository.GetClaimsForMaster(projectid, responsibleMasterId, ClaimStatusSpec.Active))
                .ToList
                ();

            return await ShowMasterClaimList(projectid, export, "Заявки на мастере", "Index", claims);
        }

        [HttpGet("~/my/claims")]
        [Authorize]
        public async Task<ActionResult> My(string? export)
        {
            var user = await GetCurrentUserAsync();
            var title = "Мои заявки";

            ViewBag.Title = title;

            var viewModel = new ClaimListViewModel(
                                       CurrentUserId,
                                       user.Claims.ToList(),
                                       projectId: null,
                                       showCount: false,
                                       showUserColumn: false);

            var exportType = ExportTypeNameParserHelper.ToExportType(export);

            if (exportType == null)
            {
                return base.View("Index", viewModel);
            }
            else
            {
                return await ExportWithCustomFrontend(
                    viewModel.Items,
                    title,
                    exportType.Value,
                    new MyClaimListItemViewModelExporter(UriService),
                    user.PrefferedName);
            }


        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> Problems(int projectId, string export)
        {
            var claims =
                (await ClaimsRepository.GetClaims(projectId, ClaimStatusSpec.Any)).Where(
                    c => c.GetProblems().Any(p => p.Severity >= ProblemSeverity.Warning)).ToList();
            return
                await ShowMasterClaimList(projectId, export, "Проблемные заявки", "Index", claims);
        }

        [HttpGet("~/{ProjectId}/claims/problems-for-master/{ResponsibleMasterId}")]
        [MasterAuthorize()]
        public async Task<ActionResult> ResponsibleProblems(int projectId, int responsibleMasterId, string export)
        {
            var claims =
                (await ClaimsRepository.GetClaimsForMaster(projectId, responsibleMasterId, ClaimStatusSpec.Any)).Where(
                    claim =>
                        claim.GetProblems().Any(p => p.Severity >= ProblemSeverity.Warning)).ToList();

            return await ShowMasterClaimList(projectId, export, "Проблемные заявки на мастере", "Index", claims);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> PaidDeclined(int projectid, string export)
        {
            var claims =
                (await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.InActive))
                .Where(claim => claim.ClaimBalance() > 0)
                .ToList();

            return await ShowMasterClaimList(projectid, export, "Оплаченные отклоненные заявки", "Index", claims);
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> ByAssignedField(int projectfieldid, int projectid, string export)
        {
            var field = await ProjectRepository.GetProjectField(projectid, projectfieldid);
            var claims = await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.Active);
            return await ShowMasterClaimList(projectid, export, "Поле (проставлено): " + field.FieldName, "Index",
                claims.Where(c => c.GetFields().Single(f => f.Field.ProjectFieldId == projectfieldid).HasEditableValue)
                    .ToList()
            );
        }

        [HttpGet, MasterAuthorize()]
        public async Task<ActionResult> ByUnAssignedField(int projectfieldid, int projectid, string export)
        {
            var field = await ProjectRepository.GetProjectField(projectid, projectfieldid);
            var claims = await ClaimsRepository.GetClaims(projectid, ClaimStatusSpec.Active);
            return await ShowMasterClaimList(projectid, export, "Поле (непроставлено): " + field.FieldName, "Index",
                claims.Where(c => !c.GetFields().Single(f => f.Field.ProjectFieldId == projectfieldid).HasEditableValue)
                    .ToList()
            );
        }
        private async Task<FileContentResult> ExportWithCustomFrontend(
            IEnumerable<ClaimListItemViewModel> viewModel, string title,
            ExportType exportType, IGeneratorFrontend<ClaimListItemViewModel> frontend, string projectName)
        {
            var generator = ExportDataService.GetGenerator(exportType, viewModel,
              frontend);

            var fileName = projectName + ": " + title;

            return File(await generator.Generate(), generator.ContentType,
                Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension));
        }
    }
}
