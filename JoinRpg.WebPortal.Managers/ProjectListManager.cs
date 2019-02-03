using System;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.WebPortal.Managers
{
    public class ProjectListManager
    {
        private ICurrentUserAccessor CurrentUser { get; }
        private readonly IProjectRepository _projectRepository;

        public ProjectListManager(IProjectRepository projectRepository, ICurrentUserAccessor currentUser)
        {
            CurrentUser = currentUser;
            _projectRepository = projectRepository;
        }

        public async Task<HomeViewModel> LoadModel(bool showInactive = false, int maxProjects = int.MaxValue)
        {
            var allProjects = showInactive
                ? await _projectRepository.GetArchivedProjectsWithClaimCount(CurrentUser.UserIdOrDefault)
                : await _projectRepository.GetActiveProjectsWithClaimCount(CurrentUser.UserIdOrDefault);

            var projects =
                allProjects
                    .Select(p => new ProjectListItemViewModel(p))
                    .Where(p => (showInactive && p.ClaimCount > 0) || p.IsMaster || p.HasMyClaims || p.IsAcceptingClaims)
                    .ToList();

            var alwaysShowProjects = ProjectListItemViewModel.OrderByDisplayPriority(
                projects.Where(p => p.IsMaster || p.HasMyClaims), p => p).ToList();

            var projectListItemViewModels = alwaysShowProjects.UnionUntilTotalCount(projects.OrderByDescending(p => p.ClaimCount), maxProjects);

            var finalProjects = projectListItemViewModels.ToList();

            return new HomeViewModel
            {
                ActiveProjects = finalProjects,
                HasMoreProjects = projects.Count > finalProjects.Count,
            };
        }
    }
}
