using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Services.Interfaces;
using MoreLinq;

namespace JoinRpg.Web.Models
{
    public class MoneyInfoForUserViewModel
    {
        public UserProfileDetailsViewModel UserDetails { get; }
        public int ProjectId { get; }
        public IReadOnlyCollection<MoneyTransferListItemViewModel> Transfers { get; }

        public FinOperationListViewModel Operations { get; }

        public MasterBalanceViewModel Balance { get; }

        public IReadOnlyCollection<PaymentTypeSummaryViewModel> PaymentTypeSummary { get; }

        public MoneyInfoForUserViewModel(Project project,
            IReadOnlyCollection<MoneyTransfer> transfers,
            User master,
            IUriService urlHelper,
            IReadOnlyCollection<FinanceOperation> operations,
            PaymentTypeSummaryViewModel[] payments,
            int currentUserId)
        {
            Transfers = transfers
                .OrderBy(f => f.Id)
                .Select(f => new MoneyTransferListItemViewModel(f, currentUserId)).ToArray();
            ProjectId = project.ProjectId;
            UserDetails = new UserProfileDetailsViewModel(master, AccessReason.CoMaster);

            Operations = new FinOperationListViewModel(project, urlHelper, operations);

            Balance = new MasterBalanceViewModel(master, project.ProjectId, operations, transfers);

            PaymentTypeSummary = payments;
        }
    }
}
