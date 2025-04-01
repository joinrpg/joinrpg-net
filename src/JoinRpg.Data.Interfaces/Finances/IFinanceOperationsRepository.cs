namespace JoinRpg.Data.Interfaces.Finances;
public interface IFinanceOperationsRepository
{
    Task<IReadOnlyCollection<FinanceOperationIdentification>> GetUnfinishedOperations(KeySetPagination<FinanceOperationIdentification>? pagination = null);
}
