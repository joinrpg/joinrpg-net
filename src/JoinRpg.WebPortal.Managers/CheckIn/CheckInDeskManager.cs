using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.WebPortal.Managers.CheckIn;

/// <summary>
/// Регистрация на стойке одной кнопкой: принять наличный взнос, затем зарегистрировать игрока.
/// </summary>
/// <remarks>
/// Это две операции (#4890): регистрация видит уже принятые деньги, а если она не прошла, деньги
/// остаются принятыми — мастер их уже взял, и форма регистрации после этого долга не показывает.
/// </remarks>
public class CheckInDeskManager(
    IClaimInfoRepository claimInfoRepository,
    IFinanceService financeService,
    IClaimService claimService,
    ICurrentUserAccessor currentUserAccessor)
{
    /// <param name="money">Принятый взнос — сумма, которую мастеру показала форма регистрации.</param>
    /// <param name="checkInOverride">Подтверждение регистрации в обход, см. <see cref="IClaimService.CheckInClaim"/>.</param>
    public async Task CheckIn(ClaimIdentification claimId, int money, CheckInOverride? checkInOverride = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(money);

        if (money > 0)
        {
            var claim = await claimInfoRepository.GetClaimInCharacterOrDefault(claimId)
                ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

            // Долга нет — значит, эту форму уже отправляли и деньги приняты: например, регистрация
            // тогда не прошла, а мастер отправил форму ещё раз. Второй раз деньги не принимаем.
            if (claim.CalculateBalance().FeeDue > 0)
            {
                var paymentType = claim.Character.ProjectInfo.ProjectFinanceSettings
                    .GetCashPaymentType(currentUserAccessor.UserIdentification)
                    ?? throw new JoinRpgInvalidUserException();

                await financeService.FeeAcceptedOperation(new FeeAcceptedOperationRequest
                {
                    ClaimId = claimId.ClaimId,
                    Contents = ".",
                    OperationDate = DateTime.UtcNow,
                    Money = money,
                    PaymentTypeId = paymentType.PaymentTypeId,
                });
            }
        }

        await claimService.CheckInClaim(claimId, checkInOverride);
    }
}
