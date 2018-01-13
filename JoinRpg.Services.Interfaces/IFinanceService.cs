using System;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
    public class SetFinanceSettingsRequest
    {
        public int ProjectId { get; set; }
        public bool WarnOnOverPayment { get; set; }
        public bool PreferentialFeeEnabled { get; set; }
        public string PreferentialFeeConditions { get; set; }
    }

    public class CreateFeeSettingRequest
    {
        public int ProjectId { get; set; }
        public int Fee { get; set; }
        public int? PreferentialFee { get; set; }
        public DateTime StartDate { get; set; }
    }

    public interface IFinanceService
  {
    Task FeeAcceptedOperation(int projectId, int claimId, string contents, DateTime operationDate,
      int feeChange, int money, int paymentTypeId);

    Task CreateCashPaymentType(int projectid, int targetUserId);
    Task TogglePaymentActivness(int projectid, int paymentTypeId);
    Task CreateCustomPaymentType(int projectId, string name, int targetMasterId);
    Task EditCustomPaymentType(int projectId, int paymentTypeId, string name, bool isDefault);
    Task CreateFeeSetting(CreateFeeSettingRequest request);
    Task DeleteFeeSetting(int projectid, int projectFeeSettingId);
    Task ChangeFee(int projectId, int claimId, int feeValue);
    Task SaveGlobalSettings(SetFinanceSettingsRequest request);
  }
}
