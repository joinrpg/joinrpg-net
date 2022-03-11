using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models
{
    public static class MasterBalanceBuilder
    {
        public static IReadOnlyCollection<MasterBalanceViewModel> ToMasterBalanceViewModels(
            IReadOnlyCollection<FinanceOperation> masterOperations,
            IReadOnlyCollection<MoneyTransfer> masterTransfers,
            int projectId)
        {
            var masters = masterOperations.Select(fo => fo.PaymentType?.User)
                .WhereNotNull()
                .Union(masterTransfers.Select(mt => mt.Receiver))
                .Union(masterTransfers.Select(mt => mt.Sender))
                .DistinctBy(master => master.UserId);


            var summary = masters.Select(master =>
                    new MasterBalanceViewModel(master, projectId, masterOperations, masterTransfers))
                .Where(fr => fr.AnythingEverHappens())
                .OrderBy(fr => fr.Master.GetDisplayName());
            return summary.ToArray();
        }
    }
}
