using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Финансовые операции над заявкой внутри мутации (ADR014). Перенос <c>ClaimImplBase.AcceptFeeImpl</c>
/// на <see cref="ClaimMutationContext"/>.
/// </summary>
internal static class ClaimFinanceOperations
{
    /// <summary>
    /// Проверяет право принять или вернуть взнос. Escape-hatch ADR014 §5: требуемое право зависит от
    /// <b>аргументов</b> операции — знака суммы и того, свой ли это тип оплаты, — поэтому его нельзя
    /// выразить константным <see cref="ClaimAccessRequirement"/>. Прямой аналог
    /// <c>RequireManageMoney</c> из ADR009.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если платёж вносит сам игрок: тогда операция считается изменением игрока и
    /// финоперация создаётся в состоянии <see cref="FinanceOperationState.Proposed"/>.
    /// </returns>
    /// <remarks>
    /// Admin-bypass'а здесь нет, как и во всём claim-контуре: <see cref="ClaimAccess"/>.
    /// </remarks>
    public static bool RequireFeeAccess(
        this ClaimMutationContext ctx,
        int money,
        PaymentTypeInfo paymentType)
    {
        paymentType.EnsureActive();

        if (money < 0)
        {
            // Возврат — не платёж игрока, его делает только распорядитель денег.
            _ = ctx.ProjectInfo.RequestMasterAccess(ctx.CurrentUser, Permission.CanManageMoney);
            return false;
        }

        if (ctx.Claim.PlayerUserId == ctx.CurrentUser.UserId
            && paymentType.User.UserId != ctx.CurrentUser.UserIdentification)
        {
            // Платит сам игрок и не на свой же тип оплаты — это заявленный, ещё не подтверждённый платёж.
            return true;
        }

        if (paymentType.User.UserId != ctx.CurrentUser.UserIdentification)
        {
            _ = ctx.ProjectInfo.RequestMasterAccess(ctx.CurrentUser, Permission.CanManageMoney);
        }
        else
        {
            _ = ctx.ProjectInfo.RequestMasterAccess(ctx.CurrentUser);
        }

        return false;
    }

    /// <summary>
    /// Принимает (или возвращает, если <paramref name="money"/> отрицательна) взнос по заявке:
    /// проверяет права и дату, создаёт комментарий с финоперацией и фиксирует взнос, если он погашен.
    /// Уведомление ставится в очередь контекста и уйдёт после сохранения.
    /// </summary>
    public static PendingComment AcceptFee(
        this ClaimMutationContext ctx,
        string contents,
        DateTime operationDate,
        int money,
        PaymentTypeInfo paymentType)
    {
        ctx.CheckOperationDate(operationDate);

        var playerChange = ctx.RequireFeeAccess(money, paymentType);

        // Активность проекта здесь не проверяется: её обеспечивает сам props-сервис параметром
        // ProjectActiveRequirement (ADR014). До миграции проверка стояла внутри помощника.

        var commentAction = money < 0 ? CommentExtraAction.RefundFee : CommentExtraAction.PaidFee;
        var claimOperationType = playerChange
            ? ClaimOperationType.PlayerChange
            : ClaimOperationType.MasterVisibleChange;
        var state = playerChange ? FinanceOperationState.Proposed : FinanceOperationState.Approved;

        // Уведомление дополняется уже после постановки в очередь — рассылка идёт после
        // SaveChanges, так что порядок «создать → дополнить» разводить по двум методам не нужно.
        var pending = ctx.AddComment(ctx.Claim, contents, commentAction, claimOperationType)
            .Decorate(notification => notification with
            {
                Money = money,
                PaymentOwner = paymentType.User,
            });

        var financeOperation = new FinanceOperation()
        {
            Created = ctx.Now,
            MoneyAmount = money,
            Changed = ctx.Now,
            Claim = ctx.Claim,
            Comment = pending.Comment,
            PaymentTypeId = paymentType.PaymentTypeId.PaymentTypeId,
            State = state,
            ProjectId = ctx.Claim.ProjectId,
            OperationDate = operationDate,
            // TODO: Remove when complete Refunds be available
            OperationType = money switch
            {
                > 0 => FinanceOperationType.Submit,
                < 0 => FinanceOperationType.Refund,
                _ => throw new PaymentException(ctx.Claim.Project, "Submit or Refund sum could not be 0"),
            },
        };

        pending.Comment.Finance = financeOperation;

        ctx.Claim.FinanceOperations.Add(financeOperation);

        ctx.Claim.UpdateClaimFeeIfRequired(operationDate, ctx.ProjectInfo);

        return pending;
    }
}
