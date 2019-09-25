using System;
using System.Threading.Tasks;
using JoinRpg.DataModel;

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
        public string Comment { get; set; }
    }

    public class ApproveRejectTransferRequest
    {
        public int ProjectId { get; set; }
        public int MoneyTranferId { get; set; }
        public bool Approved { get; set; }
    }

    /// <summary>
    /// Payload for <see cref="IFinanceService.CreatePaymentType"/>
    /// </summary>
    public class CreatePaymentTypeRequest
    {
        public int ProjectId { get; set; }
        public int? TargetMasterId { get; set; }
        public PaymentTypeKind TypeKind { get; set; }
        public string Name { get; set; }
    }


    public interface IFinanceService
    {
        Task FeeAcceptedOperation(FeeAcceptedOperationRequest request);

        /// <summary>
        /// Creates payment type for specified project
        /// </summary>
        /// <param name="request">Request payload</param>
        Task CreatePaymentType(CreatePaymentTypeRequest request);

        /// <summary>
        /// Toggles state of a payment type. If payment type has to be deactivated, it will be
        /// deleted if no payments associated with it and it could be permanently deleted
        /// </summary>
        /// <param name="projectId">Database Id of a project</param>
        /// <param name="paymentTypeId">Database Id of a payment type to toggle state of</param>
        Task TogglePaymentActiveness(int projectId, int paymentTypeId);

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
