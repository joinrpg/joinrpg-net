using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Обёртки перехода статуса на контексте операции (ADR014, §4, §7). Сами правила — таблица
/// переходов, «статус после ответа», отметки дат — проверяются в <c>DomainTypes.Test</c> и
/// <c>Domain.Test</c>; здесь — что обёртки подставляют время операции и текущего пользователя и что
/// отказ не доходит до сохранения.
/// </summary>
public class ClaimStatusTransitionTest : ClaimServiceTestBase
{
    private Claim CreateClaim(ClaimStatus status)
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        claim.ClaimStatus = status;
        mock.ReInitProjectInfo();
        return claim;
    }

    private Task ChangeStatus(Claim claim, ClaimStatus target)
        => CreatePropsService().ChangeClaim(
            claim.GetId(),
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            target,
            ctx => ctx.ChangeStatus(ctx.Claim, ctx.Request));

    /// <summary>Отметка даты и время изменения заявки — одно и то же время операции.</summary>
    [Fact]
    public async Task ChangeStatus_StampsOperationTime()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await ChangeStatus(claim, ClaimStatus.Approved);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.MasterAcceptedDate.ShouldNotBeNull();
        claim.MasterAcceptedDate.ShouldBe(claim.LastUpdateDateTime);
    }

    [Fact]
    public async Task ChangeStatus_RejectsTransition_AndSavesNothing()
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByUser);

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(() => ChangeStatus(claim, ClaimStatus.Approved));

        claim.ClaimStatus.ShouldBe(ClaimStatus.DeclinedByUser);
        SaveChangesCallCount.ShouldBe(0);
    }

    private Task MarkDiscussed(Claim claim, int currentUserId, bool isVisibleToPlayer)
        => CreatePropsService(currentUserId).ChangeClaim(
            claim.GetId(),
            ClaimAccessRequirement.MasterOrPlayer,
            ProjectActiveRequirement.MustBeActive,
            isVisibleToPlayer,
            ctx => ctx.MarkDiscussed(ctx.Request));

    /// <summary>
    /// Обёртка передаёт правилу текущего пользователя: один и тот же ответ мастера и игрока
    /// по-разному меняет статус.
    /// </summary>
    [Fact]
    public async Task MarkDiscussed_MasterReply_MovesToDiscussed()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await MarkDiscussed(claim, mock.Master.UserId, isVisibleToPlayer: true);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
    }

    /// <inheritdoc cref="MarkDiscussed_MasterReply_MovesToDiscussed"/>
    [Fact]
    public async Task MarkDiscussed_PlayerReply_KeepsStatus()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await MarkDiscussed(claim, mock.Player.UserId, isVisibleToPlayer: true);

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
    }
}
