using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.GameSubscribe;
using JoinRpg.Web.Models.Subscribe;

namespace JoinRpg.WebPortal.Managers.Subscribe
{
    public class SubscribeViewService : IGameSubscribeClient
    {
        private readonly IUriService uriService;
        private readonly IUserSubscribeRepository userSubscribeRepository;
        private readonly IFinanceReportRepository financeReportRepository;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IUserRepository userRepository;

        public SubscribeViewService(IUriService uriService,
            IUserSubscribeRepository userSubscribeRepository,
            IUserRepository userRepository,
            IFinanceReportRepository financeReportRepository,
            ICurrentUserAccessor currentUserAccessor)
        {
            this.uriService = uriService;
            this.userSubscribeRepository = userSubscribeRepository;
            this.userRepository = userRepository;
            this.financeReportRepository = financeReportRepository;
            this.currentUserAccessor = currentUserAccessor;
        }

        public async Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId)
        {
            var data = await userSubscribeRepository.LoadSubscriptionsForProject(masterId, projectId);
            var currentUser = await userRepository.GetById(currentUserAccessor.UserId);

            var paymentTypes = await financeReportRepository.GetPaymentTypesForMaster(projectId, masterId);

            return data.ToSubscribeListViewModel(currentUser, uriService, projectId, paymentTypes);
        }
    }
}
