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


    public class MarkPreferentialRequest : IClaimOperationRequest
    {
        public int ProjectId { get; set; }
        public int ClaimId { get; set; }
        public bool Preferential { get; set; }
    }

    public class FeeAcceptedOperationRequest : IClaimOperationRequest
    {
        public int ProjectId { get; set; }
        public int ClaimId { get; set; }
        public string Contents { get; set; }
        public DateTime OperationDate { get; set; }
        public int FeeChange { get; set; }
        public int Money { get; set; }
        public int PaymentTypeId { get; set; }
    }

    public class MarkMeAsPreferentialFeeOperationRequest : IClaimOperationRequest
    {
        public int ProjectId { get; set; }
        public int ClaimId { get; set; }
        public string Contents { get; set; }
        public DateTime OperationDate { get; set; }
    }

    public class CreateTransferRequest
    {
        public int ProjectId { get; set; }
        public int Sender { get; set; }
        public int Receiver { get; set; }
        public int Amount { get; set; }
        public DateTime OperationDate { get; set; }
    }

    public class ApproveRejectTransferRequest
    {
        public int ProjectId { get; set; }
        public int MoneyTranferId { get; set; }
        public bool Approved { get; set; }
    }

    public interface IFinanceService
    {
        Task FeeAcceptedOperation(FeeAcceptedOperationRequest request);

        Task CreateCashPaymentType(int projectid, int targetUserId);
        Task TogglePaymentActivness(int projectid, int paymentTypeId);
        Task CreateCustomPaymentType(int projectId, string name, int targetMasterId);
        Task EditCustomPaymentType(int projectId, int paymentTypeId, string name, bool isDefault);
        Task CreateFeeSetting(CreateFeeSettingRequest request);
        Task DeleteFeeSetting(int projectid, int projectFeeSettingId);
        Task ChangeFee(int projectId, int claimId, int feeValue);
        Task SaveGlobalSettings(SetFinanceSettingsRequest request);
        Task MarkPreferential(MarkPreferentialRequest request);
        Task RequestPreferentialFee(MarkMeAsPreferentialFeeOperationRequest request);

        Task CreateTransfer(CreateTransferRequest request);
        Task MarkTransfer(ApproveRejectTransferRequest request);
    }
}
