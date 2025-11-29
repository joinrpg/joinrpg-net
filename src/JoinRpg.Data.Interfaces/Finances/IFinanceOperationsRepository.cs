using JoinRpg.Interfaces;

namespace JoinRpg.Data.Interfaces.Finances;
public interface IFinanceOperationsRepository
{
    Task<IReadOnlyCollection<FinanceOperationIdentification>> GetUnfinishedOperations(KeySetPagination? pagination = null);
}
