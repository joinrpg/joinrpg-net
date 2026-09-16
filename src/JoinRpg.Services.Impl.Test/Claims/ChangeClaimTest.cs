using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Тесты общей механики <c>ChangeClaim</c> (ADR014): разворачивание требования доступа и отложенная
/// рассылка уведомлений после сохранения.
/// </summary>
public class ChangeClaimTest : ClaimServiceTestBase
{
    private ClaimIdentification CreateClaim()
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        mock.ReInitProjectInfo();
        return claim.GetId();
    }

    [Fact]
    public async Task Notification_IsSentAfterSave_WithCommentId()
    {
        var claimId = CreateClaim();

        await CreatePropsService().ChangeClaim(
            claimId,
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            "текст",
            ctx => ctx.AddComment(ctx.Request, CommentExtraAction.FeeChanged, ClaimOperationType.MasterVisibleChange));

        SaveChangesCallCount.ShouldBe(1);
        claimNotifications.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SilentComment_CreatesComment_ButSendsNothing()
    {
        var claimId = CreateClaim();

        await CreatePropsService().ChangeClaim(
            claimId,
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            "текст",
            ctx => ctx.AddComment(ctx.Request, CommentExtraAction.FeeChanged, ClaimOperationType.MasterVisibleChange).Silent());

        SaveChangesCallCount.ShouldBe(1);
        claimNotifications.Sent.ShouldBeEmpty();
    }

    /// <summary>
    /// Если мутация упала, уведомления не уходят и ничего не сохраняется. До миграции это
    /// приходилось соблюдать вручную в каждом методе.
    /// </summary>
    [Fact]
    public async Task WhenMutationThrows_NothingIsSavedOrSent()
    {
        var claimId = CreateClaim();

        await Should.ThrowAsync<InvalidOperationException>(
            () => CreatePropsService().ChangeClaim(
                claimId,
                ClaimAccessRequirement.AnyMaster,
                ProjectActiveRequirement.MustBeActive,
                "текст",
                ctx =>
                {
                    _ = ctx.AddComment(ctx.Request, CommentExtraAction.FeeChanged, ClaimOperationType.MasterVisibleChange);
                    throw new InvalidOperationException();
                }));

        SaveChangesCallCount.ShouldBe(0);
        claimNotifications.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnyMaster_RejectsPlayer()
    {
        var claimId = CreateClaim();

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreatePropsService(mock.Player.UserId).ChangeClaim(
                claimId,
                ClaimAccessRequirement.AnyMaster,
                ProjectActiveRequirement.MustBeActive,
                0,
                ctx => { }));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task MasterOrPlayer_AllowsPlayer()
    {
        var claimId = CreateClaim();

        await CreatePropsService(mock.Player.UserId).ChangeClaim(
            claimId,
            ClaimAccessRequirement.MasterOrPlayer,
            ProjectActiveRequirement.MustBeActive,
            0,
            ctx => { });

        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task PlayerOnly_RejectsMaster()
    {
        var claimId = CreateClaim();

        await Should.ThrowAsync<PlayerOnlyException>(
            () => CreatePropsService().ChangeClaim(
                claimId,
                ClaimAccessRequirement.PlayerOnly,
                ProjectActiveRequirement.MustBeActive,
                0,
                ctx => { }));

        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Единственное требование, зависящее от данных: у неутверждённой заявки игрок поселение менять
    /// не может, у утверждённой — может.
    /// </summary>
    [Fact]
    public async Task AccommodationChange_DependsOnClaimStatus()
    {
        var character = mock.CreateCharacter("Вася");
        var pending = mock.CreateClaim(character, mock.Player);
        mock.ReInitProjectInfo();

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreatePropsService(mock.Player.UserId).ChangeClaim(
                pending.GetId(),
                ClaimAccessRequirement.AccommodationChange,
                ProjectActiveRequirement.MustBeActive,
                0,
                ctx => { }));

        var approvedCharacter = mock.CreateCharacter("Петя");
        var approved = mock.CreateApprovedClaim(approvedCharacter, mock.Player);
        mock.ReInitProjectInfo();

        await CreatePropsService(mock.Player.UserId).ChangeClaim(
            approved.GetId(),
            ClaimAccessRequirement.AccommodationChange,
            ProjectActiveRequirement.MustBeActive,
            0,
            ctx => { });

        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Сторож реентерабельности (ADR014, §7): вход в мутацию из середины другой мутации того же
    /// экземпляра падает сразу. Именно так до миграции работал автоприём — и именно поэтому
    /// <c>Now</c> и <c>CurrentUserId</c> расходились с операцией.
    /// </summary>
    [Fact]
    public async Task NestedMutationOnSameInstance_Throws_AndNothingIsSaved()
    {
        var claimId = CreateClaim();
        var propsService = CreatePropsService();

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => propsService.ChangeClaimAsync(
                claimId,
                ClaimAccessRequirement.AnyMaster,
                ProjectActiveRequirement.MustBeActive,
                0,
                async ctx => await propsService.ChangeClaim(
                    claimId,
                    ClaimAccessRequirement.AnyMaster,
                    ProjectActiveRequirement.MustBeActive,
                    0,
                    nested => { })));

        // Сообщение общего сторожа (ADR009/ADR014) про ADR не говорит — он один на оба
        // props-сервиса, поэтому проверяем, что названы операция и экземпляр.
        exception.Message.ShouldContain(nameof(CharacterPropsService));
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Сторож опускается в <c>finally</c>: упавшая операция не блокирует экземпляр навсегда.
    /// </summary>
    [Fact]
    public async Task AfterFailedMutation_SameInstanceStillUsable()
    {
        var claimId = CreateClaim();
        var propsService = CreatePropsService();

        await Should.ThrowAsync<InvalidOperationException>(
            () => propsService.ChangeClaim(
                claimId,
                ClaimAccessRequirement.AnyMaster,
                ProjectActiveRequirement.MustBeActive,
                0,
                ctx => throw new InvalidOperationException("упало")));

        await propsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            0,
            ctx => { });

        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Операция, объявившая себя холостой, не сохраняет и не рассылает — иначе «поменять на то же
    /// самое» начало бы шуметь комментариями.
    /// </summary>
    [Fact]
    public async Task NothingChanged_SkipsSave()
    {
        var claimId = CreateClaim();

        await CreatePropsService().ChangeClaim(
            claimId,
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            0,
            ctx => ctx.NothingChanged());

        SaveChangesCallCount.ShouldBe(0);
        claimNotifications.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task ArchivedProject_Throws()
    {
        var claimId = CreateClaim();
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreatePropsService().ChangeClaim(
                claimId,
                ClaimAccessRequirement.AnyMaster,
                ProjectActiveRequirement.MustBeActive,
                0,
                ctx => { }));

        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Операции, которым архив разрешён явно, продолжают работать. Сегодня это комментирование,
    /// отметка «прочитано» и платёжные колбэки (ADR014).
    /// </summary>
    [Fact]
    public async Task ArchivedProject_AllowInactive_Works()
    {
        var claimId = CreateClaim();
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        await CreatePropsService().ChangeClaim(
            claimId,
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.AllowInactive,
            0,
            ctx => { });

        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Вложенная мутация на том же экземпляре сервиса запрещена (ADR014, §7): у внешней операции
    /// уже зафиксировано время, а у вложенной может оказаться другой текущий пользователь.
    /// </summary>
    [Fact]
    public async Task NestedMutation_OnSameInstance_Throws()
    {
        var claimId = CreateClaim();
        var service = CreatePropsService();

        Task nested = null!;

        await service.ChangeClaim(
            claimId,
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            0,
            // Сторож опускается до первого await, поэтому задача уже сломана — достаточно её забрать.
            ctx => nested = service.ChangeClaim(
                claimId,
                ClaimAccessRequirement.AnyMaster,
                ProjectActiveRequirement.MustBeActive,
                0,
                _ => { }));

        _ = await Should.ThrowAsync<InvalidOperationException>(() => nested);
    }
}
