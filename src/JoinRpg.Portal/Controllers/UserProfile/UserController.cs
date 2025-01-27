using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.ClaimList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

public class UserController : Common.ControllerBase
{
    private readonly IProblemValidator<Claim> claimValidator;
    private readonly IProjectMetadataRepository projectMetadataRepository;

    public IUserRepository UserRepository { get; }
    public ICurrentUserAccessor CurrentUserAccessor { get; }

    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult> Details(int userId)
    {
        var user = await UserRepository.GetById(userId);

        var currentUser = User.Identity?.IsAuthenticated == true ? await UserRepository.GetById(CurrentUserAccessor.UserId) : null;

        var userProfileViewModel = new UserProfileViewModel()
        {
            DisplayName = user.GetDisplayName(),
            ThisUserProjects = user.ProjectAcls.Select(p => p.Project).ToLinkViewModels().ToList(),
            UserId = user.UserId,
            Details = new UserProfileDetailsViewModel(user, currentUser),
            HasAdminAccess = CurrentUserAccessor.IsAdmin,
            IsAdmin = user.Auth.IsAdmin,
        };

        if (currentUser != null)
        {
            userProfileViewModel.CanGrantAccessProjects =
                currentUser.GetProjects(acl => acl.CanGrantRights).Where(project => project.Active).ToLinkViewModels().ToList();
            var claims = CurrentUserAccessor.IsAdmin
                ? user.Claims.ToArray()
                : user.Claims.Where(claim => claim.HasAccess(CurrentUserAccessor.UserId, ExtraAccessReason.Player)).ToArray();

            var projectInfos = new List<ProjectInfo>();
            foreach (var projectId in claims.Select(c => c.ProjectId).Distinct())
            {
                projectInfos.Add(await projectMetadataRepository.GetProjectMetadata(new(projectId)));
            }
            userProfileViewModel.Claims = new ClaimListViewModel(CurrentUserAccessor.UserId,
                claims,
                null,
                new Dictionary<int, int>(), //TODO pass unread data here
                claimValidator,
                projectInfos, showCount: false,
                showUserColumn: false);
        }

        return View(userProfileViewModel);
    }

    public UserController(IUserRepository userRepository, ICurrentUserAccessor currentUserAccessor, IProblemValidator<Claim> claimValidator, IProjectMetadataRepository projectMetadataRepository)
        : base()
    {
        UserRepository = userRepository;
        CurrentUserAccessor = currentUserAccessor;
        this.claimValidator = claimValidator;
        this.projectMetadataRepository = projectMetadataRepository;
    }

    [Authorize]
    [HttpGet("user/me")]
    public ActionResult Me() => RedirectToAction("Details", new { CurrentUserAccessor.UserId });
}
