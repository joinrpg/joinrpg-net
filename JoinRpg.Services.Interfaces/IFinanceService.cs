using System;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IFinanceService
  {
    Task FeeAcceptedOperation(int projectId, int claimId, int currentUserId, string contents, DateTime operationDate,
      int feeChange, int money, int paymentTypeId);
  }
}
