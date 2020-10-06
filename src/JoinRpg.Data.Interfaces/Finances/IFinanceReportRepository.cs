using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel.Finances;

namespace JoinRpg.Data.Interfaces
{
    public interface IFinanceReportRepository
    {
        Task<List<MoneyTransfer>> GetMoneyTransfersForMaster(int projectId, int masterId);
        Task<List<MoneyTransfer>> GetAllMoneyTransfers(int projectId);

        /// <summary>
        /// Get all payment types which linked to master to project
        /// </summary>
        Task<List<PaymentTypeDto>> GetPaymentTypesForMaster(int projectId, int masterId);
    }
}
