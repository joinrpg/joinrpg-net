using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Закрепляет два свойства <c>ChangeClaim</c>, из-за которых контур онлайн-оплат
/// (<c>PaymentsService</c>) на него <b>не переведён</b>: у входящего платёжного колбэка нет
/// пользователя, а ночная сверка идёт под роботом-админом без ACL в проекте.
/// </summary>
/// <remarks>
/// Тесты здесь не про <c>PaymentsService</c>, а про механику доступа, на которую опирается решение
/// «не мигрировать». Если однажды в <c>ICharacterPropsService</c> появится системный вход без
/// пользователя, эти тесты придётся переписать осознанно — и это ровно тот момент, когда контур
/// оплат можно будет переводить.
/// </remarks>
public class PaymentCallbackAccessTest : ClaimServiceTestBase
{
    /// <summary>Аноним: ровно то, чем является банк, вернувший плательщика на наш колбэк.</summary>
    private sealed class AnonymousUserAccessor : ICurrentUserAccessor
    {
        public int? UserIdOrDefault => null;
        public UserDisplayName DisplayName => new("Аноним", null);
        public bool IsAdmin => false;
        public AvatarIdentification? Avatar => null;
    }

    private ClaimIdentification CreateClaim()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        mock.ReInitProjectInfo();
        return claim.GetId();
    }

    /// <summary>
    /// <c>ClaimPaymentSuccess</c> и <c>ClaimPaymentFail</c> объявлены без <c>[Authorize]</c>:
    /// платёжная система возвращает плательщика без нашей сессии. <c>ChangeClaim</c> в такой
    /// ситуации падает ещё до проверки прав — поэтому колбэк через него провести нельзя, сколько бы
    /// <c>AllowInactive</c> ему ни ставили.
    /// </summary>
    [Fact]
    public async Task AnonymousUser_CannotEnterChangeClaim()
    {
        var claimId = CreateClaim();

        var exception = await Should.ThrowAsync<Exception>(
            () => CreatePropsService(new AnonymousUserAccessor()).ChangeClaim(
                claimId,
                ClaimAccessRequirement.MasterOrPlayer,
                ProjectActiveRequirement.AllowInactive,
                0,
                ctx => { }));

        exception.Message.ShouldContain("Authorization");
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Ночные джобы (<c>UpdatePaymentStatusJob</c>, <c>PerformRecurrentPaymentMidnightJob</c>) идут
    /// под роботом с флагом администратора, но без ACL в проекте. В claim-путях admin-bypass'а нет
    /// намеренно (ADR014 §5), поэтому робот получит отказ — в отличие от
    /// <c>ProjectPropsService</c>, где такой bypass есть.
    /// </summary>
    [Fact]
    public async Task AdminWithoutProjectAcl_IsStillRejected()
    {
        var claimId = CreateClaim();

        // Игрок — известный моку пользователь, не входящий в ACL проекта.
        await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreatePropsService(mock.Player.UserId, isAdmin: true).ChangeClaim(
                claimId,
                ClaimAccessRequirement.AnyMaster,
                ProjectActiveRequirement.AllowInactive,
                0,
                ctx => { }));

        SaveChangesCallCount.ShouldBe(0);
    }
}
