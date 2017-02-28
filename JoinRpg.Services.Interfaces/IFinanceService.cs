using System;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IFinanceService
  {
    Task FeeAcceptedOperation(int projectId, int claimId, string contents, DateTime operationDate,
      int feeChange, int money, int paymentTypeId);

    Task CreateCashPaymentType(int projectid, int targetUserId);
    Task TogglePaymentActivness(int projectid, int paymentTypeId);
    Task CreateCustomPaymentType(int projectId, string name, int targetMasterId);
    Task EditCustomPaymentType(int projectId, int paymentTypeId, string name, bool isDefault);
    Task CreateFeeSetting(int projectId, int fee, DateTime startDate);
    Task DeleteFeeSetting(int projectid, int projectFeeSettingId);
    Task ChangeFee(int projectId, int claimId, int feeValue);
    Task SaveGlobalSettings(int projectId, bool warnOnOverPayment);
  }
}
