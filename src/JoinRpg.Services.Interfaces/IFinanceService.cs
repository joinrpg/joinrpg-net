using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Services.Interfaces;

public class MarkPreferentialRequest : IClaimOperationRequest
{
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }
    public bool Preferential { get; set; }
}

public class FeeAcceptedOperationRequest : IClaimOperationRequest
{
    int IClaimOperationRequest.ProjectId => PaymentTypeId.ProjectId;
    public int ClaimId { get; set; }
    public required string Contents { get; set; }
    public DateTime OperationDate { get; set; }
    public int FeeChange { get; set; }
    public int Money { get; set; }
    public required PaymentTypeIdentification PaymentTypeId { get; set; }
}

public class MarkMeAsPreferentialFeeOperationRequest : IClaimOperationRequest
{
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }
    public required string Contents { get; set; }
    public DateTime OperationDate { get; set; }
}

public class CreateTransferRequest
{
    public int ProjectId { get; set; }
    public required UserIdentification Sender { get; set; }
    public required UserIdentification Receiver { get; set; }
    public int Amount { get; set; }
    public DateTime OperationDate { get; set; }
    public required string Comment { get; set; }
}

public class ApproveRejectTransferRequest
{
    public int ProjectId { get; set; }
    public int MoneyTranferId { get; set; }
    public bool Approved { get; set; }
}

/// <summary>
/// Payload for <see cref="IFinanceService.TransferPaymentAsync"/>
/// </summary>
public class ClaimPaymentTransferRequest : ClaimPaymentRequest, IClaimOperationRequest
{
    /// <summary>
    /// Claim to transfer money to
    /// </summary>
    public required int ToClaimId { get; set; }
}



public interface IFinanceService
{
    Task FeeAcceptedOperation(FeeAcceptedOperationRequest request);

    /// <summary>
    /// Transfers money from one claim to another
    /// </summary>
    /// <param name="request">Request data</param>
    Task TransferPaymentAsync(ClaimPaymentTransferRequest request);

    Task ChangeFee(ClaimIdentification claimIdentification, int feeValue);
    Task MarkPreferential(MarkPreferentialRequest request);
    Task RequestPreferentialFee(MarkMeAsPreferentialFeeOperationRequest request);

    Task CreateTransfer(CreateTransferRequest request);
    Task MarkTransfer(ApproveRejectTransferRequest request);
}
