using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel.Finances;

namespace JoinRpg.Data.Interfaces
{
    public interface IFinanceReportRepository
    {
        Task<List<MoneyTransfer>> GetMoneyTransfersForMaster(int projectId, int masterId);
        Task<List<MoneyTransfer>> GetAllMoneyTransfers(int projectId);
    }
}
