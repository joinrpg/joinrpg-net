using System;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IFinanceService
  {
    Task FeeAcceptedOperation(int projectId, int claimId, int currentUserId, string contents, DateTime operationDate,
      int feeChange, int money, int paymentTypeId);

    Task CreateCashPaymentType(int projectid, int currentUserId, int targetUserId);
    Task TogglePaymentActivness(int projectid, int currentUserId, int paymentTypeId);
    Task CreateCustomPaymentType(int projectId, int currentUserId, string name, int targetMasterId);
    Task EditCustomPaymentType(int projectId, int currentUserId, int paymentTypeId, string name, bool isDefault);
  }
}
