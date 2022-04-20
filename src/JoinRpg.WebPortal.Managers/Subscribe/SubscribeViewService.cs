using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Subscribe;
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
        private readonly IGameSubscribeService gameSubscribeService;
        private readonly IUserRepository userRepository;

        public SubscribeViewService(IUriService uriService,
            IUserSubscribeRepository userSubscribeRepository,
            IUserRepository userRepository,
            IFinanceReportRepository financeReportRepository,
            ICurrentUserAccessor currentUserAccessor,
            IGameSubscribeService gameSubscribeService)
        {
            this.uriService = uriService;
            this.userSubscribeRepository = userSubscribeRepository;
            this.userRepository = userRepository;
            this.financeReportRepository = financeReportRepository;
            this.currentUserAccessor = currentUserAccessor;
            this.gameSubscribeService = gameSubscribeService;
        }

        public async Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId)
        {
            var data = await userSubscribeRepository.LoadSubscriptionsForProject(masterId, projectId);
            var currentUser = await userRepository.GetById(currentUserAccessor.UserId);

            var paymentTypes = await financeReportRepository.GetPaymentTypesForMaster(projectId, masterId);

            return data.ToSubscribeListViewModel(currentUser, uriService, projectId, paymentTypes);
        }

        public async Task RemoveSubscription(int projectId, int userSubscriptionsId)
        {
            await gameSubscribeService.RemoveSubscribe(new RemoveSubscribeRequest(projectId, userSubscriptionsId));
        }
    }
}
