namespace JoinRpg.Web.Claims.Finance;

/// <summary>
/// Подтверждение и отклонение переводов денег между мастерами.
/// </summary>
public interface IMoneyTransferClient
{
    Task Approve(MoneyTransferIdentification transferId);

    Task Decline(MoneyTransferIdentification transferId);
}
