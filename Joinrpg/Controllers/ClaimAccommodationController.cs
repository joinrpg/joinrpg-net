using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Controllers
{
    public class ClaimAccommodationController : ControllerGameBase
    {
        private readonly IClaimsRepository _claimsRepository;
        private readonly IAccommodationRequestRepository _accommodationRequestRepository;
        private readonly IAccommodationRepository _accommodationRepository;

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Index(int projectId, int claimId) 
        {
            var claim = await _claimsRepository.GetClaim(projectId, claimId).ConfigureAwait(false);
            var currentUser = await GetCurrentUserAsync().ConfigureAwait(false);
            var availableAccommodation = await
                _accommodationRepository.GetPlayerSelectableAccommodationForProject(projectId).ConfigureAwait(false);
            var requestForAccommodation = await _accommodationRequestRepository

                .GetAccommodationRequestForClaim(claimId).ConfigureAwait(false);
            var claimAccommodationViewModel = new ClaimAccommodationViewModel
            {
                ClaimId = claimId,
                ProjectId = projectId,
                AvailableAccommodationTypes = availableAccommodation,
                AccommodationRequests = requestForAccommodation,
                Navigation = CharacterNavigationViewModel.FromClaim(claim,
                    currentUser.UserId,
                    CharacterNavigationPage.Accommodation)
            };
          
            return View(claimAccommodationViewModel);
        }

        public ClaimAccommodationController(ApplicationUserManager userManager,
            IProjectRepository projectRepository,
            IProjectService projectService,
            IClaimsRepository claimsRepository,
            IAccommodationRequestRepository accommodationRequestRepository,
            IExportDataService exportDataService,
            IAccommodationRepository accommodationRepository)
            : base(userManager, projectRepository, projectService, exportDataService)
        {
            _claimsRepository = claimsRepository;
            _accommodationRequestRepository = accommodationRequestRepository;
            _accommodationRepository = accommodationRepository;
        }

    }
}
