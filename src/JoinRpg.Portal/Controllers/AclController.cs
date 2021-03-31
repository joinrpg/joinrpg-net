using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [RequireMasterOrAdmin()]
    [Route("{projectId}/masters")]
    public class AclController : ControllerGameBase
    {
        private readonly ICurrentUserAccessor currentUserAccessor;

        private IClaimsRepository ClaimRepository { get; }
        private IUriService UriService { get; }

        [HttpGet("add/{userId}")]
        [MasterAuthorize(Permission.CanGrantRights)]
        public async Task<ActionResult> Add(int projectId, int userId)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            var currentUser = await UserRepository.GetById(currentUserAccessor.UserId);
            var targetUser = await UserRepository.GetById(userId);
            var projectAcl = project.ProjectAcls.Single(acl => acl.UserId == currentUserAccessor.UserId);

            return View(AclViewModel.New(projectAcl, currentUser, targetUser));
        }

        [HttpPost("add/{userId}")]
        [MasterAuthorize(Permission.CanGrantRights)]
        public async Task<ActionResult> Add(AclViewModel viewModel)
        {
            try
            {
                await ProjectService.GrantAccess(new GrantAccessRequest()
                {
                    ProjectId = viewModel.ProjectId,
                    UserId = viewModel.UserId,
                    CanGrantRights = viewModel.CanGrantRights,
                    CanChangeFields = viewModel.CanChangeFields,
                    CanChangeProjectProperties = viewModel.CanChangeProjectProperties,
                    CanManageClaims = viewModel.CanManageClaims,
                    CanEditRoles = viewModel.CanEditRoles,
                    CanManageMoney = viewModel.CanManageMoney,
                    CanSendMassMails = viewModel.CanSendMassMails,
                    CanManagePlots = viewModel.CanManagePlots,
                    CanManageAccommodation = viewModel.CanManageAccommodation,
                    CanSetPlayersAccommodations = viewModel.CanSetPlayersAccommodations,
                });
            }
            catch
            {
                //TODO Fix this.
                ModelState.AddModelError("", "Error!");
                return RedirectToAction("Details", "User", new { viewModel.UserId });
            }

            return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });
        }

        public AclController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IClaimsRepository claimRepository,
            IUriService uriService,
            IUserRepository userRepository,
            ICurrentUserAccessor currentUserAccessor)
            : base(projectRepository, projectService, userRepository)
        {
            ClaimRepository = claimRepository;
            UriService = uriService;
            this.currentUserAccessor = currentUserAccessor;
        }

        [RequireMasterOrAdmin()]
        [HttpGet]
        public async Task<ActionResult> Index(int projectId)
        {
            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
            var claims = await ClaimRepository.GetClaimsCountByMasters(projectId, ClaimStatusSpec.Active);
            var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);
            var currentUser = await GetCurrentUserAsync();

            return View(new MastersListViewModel(project, claims, groups, currentUser, UriService));
        }

        [HttpGet("delete")]
        [MasterAuthorize(Permission.CanGrantRights)]
        public async Task<ActionResult> Delete(int projectId, int projectaclid)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            var projectAcl = project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid);
            var claims = await ClaimRepository.GetClaimsForMaster(projectId, projectAcl.UserId, ClaimStatusSpec.Any);
            var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);

            var viewModel = DeleteAclViewModel.FromAcl(projectAcl, claims.Count,
              groups.Where(gr => gr.ResponsibleMasterUserId == projectAcl.UserId).ToList(),
              UriService);
            if (viewModel.UserId == CurrentUserId)
            {
                viewModel.SelfRemove = true;
            }
            return View("Delete", viewModel);

        }

        [HttpPost("delete")]
        [ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
        public async Task<ActionResult> Delete(DeleteAclViewModel viewModel)
        {
            try
            {
                await ProjectService.RemoveAccess(viewModel.ProjectId, viewModel.UserId, viewModel.ResponsibleMasterId);
            }
            catch
            {
                return View(viewModel);
            }
            if (viewModel.UserId == CurrentUserId)
            {
                //We are removing ourself, need to redirect to public page
                return await RedirectToProject(viewModel.ProjectId);
            }
            return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

        }

        [HttpGet("leave")]
        [RequireMaster()]
        public async Task<ActionResult> RemoveYourself(int projectId, int projectAclId) => await Delete(projectId, projectAclId);

        [HttpPost("leave")]
        [ValidateAntiForgeryToken, RequireMaster()]
        public async Task<ActionResult> RemoveYourself(DeleteAclViewModel viewModel) => await Delete(viewModel);


        [HttpGet("edit")]
        [MasterAuthorize(Permission.CanGrantRights)]
        public async Task<ActionResult> Edit(int projectId, int? projectaclid)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);
            var projectAcl = project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid);
            var currentUser = await GetCurrentUserAsync();
            return View(AclViewModel.FromAcl(projectAcl, 0,
              groups.Where(gr => gr.ResponsibleMasterUserId == projectAcl.UserId).ToList(), currentUser,
                UriService));
        }

        [HttpPost("edit")]
        [ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
        public async Task<ActionResult> Edit(ChangeAclViewModel viewModel)
        {
            try
            {
                await ProjectService.ChangeAccess(new ChangeAccessRequest()
                {
                    ProjectId = viewModel.ProjectId,
                    UserId = viewModel.UserId,
                    CanGrantRights = viewModel.CanGrantRights,
                    CanChangeFields = viewModel.CanChangeFields,
                    CanChangeProjectProperties = viewModel.CanChangeProjectProperties,
                    CanManageClaims = viewModel.CanManageClaims,
                    CanEditRoles = viewModel.CanEditRoles,
                    CanManageMoney = viewModel.CanManageMoney,
                    CanSendMassMails = viewModel.CanSendMassMails,
                    CanManagePlots = viewModel.CanManagePlots,
                    CanManageAccommodation = viewModel.CanManageAccommodation,
                    CanSetPlayersAccommodations = viewModel.CanSetPlayersAccommodations,
                });
            }
            catch
            {
                //TODO Fix this
                return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });
            }
            return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

        }

        [AdminAuthorize]
        [HttpGet("force-admin-access")]
        public ActionResult ForceSet(int projectid) => View();

        [AdminAuthorize]
        [HttpPost("force-admin-access")]
        public async Task<ActionResult> ForceSet(int projectId, [UsedImplicitly] IFormCollection unused)
        {
            await ProjectService.GrantAccessAsAdmin(projectId);
            return RedirectToAction("Details", "Game", new { projectId });
        }
    }
}
