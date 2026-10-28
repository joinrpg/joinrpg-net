using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Comments;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using CommentExtraAction = JoinRpg.DomainTypes.Characters.Claims.CommentExtraAction;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Запросы <see cref="ICommentDiscussionRepository"/> на живой БД (ADR026 §2): проекция
/// переводится в SQL, владелец находится обратным запросом, комментарии без финоперации и текста не
/// теряются.
/// </summary>
/// <remarks>
/// <c>CommentDiscussionMapperTest</c> проверяет только маппинг готовых строк. Перевод проекции в
/// SQL — с OUTER APPLY на заявку, тему, текст и финоперацию — проверяется только здесь.
/// </remarks>
public class CommentDiscussionRepositoryScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task ClaimDiscussion_LoadsCommentsFinanceAndOwner()
    {
        UserIdentification masterId;
        UserIdentification playerId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для снимка обсуждения");
        }

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = masterId,
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            });

            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));

        // Платёж игрока на кассу мастера создаётся неподтверждённым — со способом оплаты.
        var cashPaymentTypeId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            var paymentTypeId = projectInfo.ProjectFinanceSettings.GetCashPaymentType(masterId)!.PaymentTypeId;
            await sp.GetRequiredService<IFinanceService>().FeeAcceptedOperation(new FeeAcceptedOperationRequest
            {
                ClaimId = claimId.ClaimId,
                Contents = "Отдал наличными",
                OperationDate = DateTime.UtcNow.Date,
                Money = 500,
                PaymentTypeId = paymentTypeId,
            });
            return paymentTypeId;
        });

        var discussionId = new CommentDiscussionId(GetClaimDiscussionId(claimId));

        await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IClaimService>().AddComment(claimId, parentCommentId: null, isVisibleToPlayer: false, "Скрытый комментарий"));
        var hiddenId = GetCommentId(discussionId, "Скрытый комментарий");
        await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IClaimService>().AddComment(claimId, hiddenId.Value, isVisibleToPlayer: false, "Ответ на скрытый"));
        var replyId = GetCommentId(discussionId, "Ответ на скрытый");

        using var scope2 = factory.Services.CreateScope();
        var repository = scope2.ServiceProvider.GetRequiredService<ICommentDiscussionRepository>();

        var discussion = (await repository.GetDiscussionOrDefault(discussionId)).ShouldNotBeNull();

        var claimOwner = new ClaimDiscussionOwner(claimId);
        discussion.Id.ShouldBe(discussionId);
        discussion.Owner.ShouldBe(claimOwner);
        // Все комментарии на месте: OUTER APPLY к финоперации не выкинул комментарии без неё.
        discussion.Comments.Select(c => c.Id).ShouldBe(GetCommentIds(discussionId), ignoreOrder: true);

        var claimCreated = discussion.Comments.Single(c => c.ExtraAction == CommentExtraAction.NewClaim);
        claimCreated.IsCommentByPlayer.ShouldBeTrue();
        claimCreated.Author.ShouldBe(playerId);
        claimCreated.Finance.ShouldBeNull();

        discussion.Comments.ShouldContain(c => c.ExtraAction == CommentExtraAction.ApproveByMaster && c.Author == masterId);

        var payment = discussion.Comments.Single(c => c.Finance != null);
        payment.Text.ShouldBe(new MarkdownString("Отдал наличными"));
        var finance = payment.Finance.ShouldNotBeNull();
        finance.MoneyAmount.ShouldBe(500);
        finance.State.ShouldBe(FinanceOperationState.Proposed);
        finance.PaymentType.ShouldBe(cashPaymentTypeId);

        var hidden = discussion.Comments.Single(c => c.Id == hiddenId);
        hidden.IsVisibleToPlayer.ShouldBeFalse();
        hidden.ParentCommentId.ShouldBeNull();
        hidden.Text.ShouldBe(new MarkdownString("Скрытый комментарий"));

        var reply = discussion.Comments.Single(c => c.Id == replyId);
        reply.ParentCommentId.ShouldBe(hiddenId);
        reply.IsVisibleToPlayer.ShouldBeFalse();

        (await repository.GetOwnerOrDefault(discussionId)).ShouldBe(claimOwner);
        // Финоперация адресуется id своего комментария — отдельного метода для неё нет.
        (await repository.GetOwnerByCommentOrDefault(payment.Id)).ShouldBe(claimOwner);

        (await repository.GetDiscussionOrDefault(new CommentDiscussionId(int.MaxValue))).ShouldBeNull();
        (await repository.GetOwnerOrDefault(new CommentDiscussionId(int.MaxValue))).ShouldBeNull();
        (await repository.GetOwnerByCommentOrDefault(new CommentId(int.MaxValue))).ShouldBeNull();
    }

    [Fact]
    public async Task ReadWatermark_IsMaximumOfUserMarks()
    {
        UserIdentification masterId;
        UserIdentification otherUserId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            otherUserId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для отметок прочтения");
        }

        var threadId = await CreateThreadAsync(masterId, projectId, hideFromUser: false);
        var discussionId = new CommentDiscussionId(GetThreadDiscussionId(threadId));
        await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IForumService>().AddComment(threadId, parentCommentId: null, isVisibleToPlayer: true, "Второй"));
        var firstId = GetCommentId(discussionId, "Первый комментарий в теме");
        var secondId = GetCommentId(discussionId, "Второй");

        using (var scope = factory.Services.CreateScope())
        {
            (await scope.ServiceProvider.GetRequiredService<ICommentDiscussionRepository>()
                .GetReadWatermark(discussionId, masterId)).ShouldBeNull();
        }

        // Отметки дублируются в БД (сервис чистит дубли только при следующей записи) — берётся максимум.
        // Максимум посередине: так тест отличает его и от первой, и от последней вставленной.
        AddWatermark(projectId, masterId, discussionId, firstId);
        AddWatermark(projectId, masterId, discussionId, secondId);
        AddWatermark(projectId, masterId, discussionId, firstId);

        // Отметка того же пользователя в другом обсуждении — на более позднем комментарии — не в счёт.
        var otherThreadId = await CreateThreadAsync(masterId, projectId, hideFromUser: false);
        var otherDiscussionId = new CommentDiscussionId(GetThreadDiscussionId(otherThreadId));
        var otherCommentId = GetCommentId(otherDiscussionId, "Первый комментарий в теме");
        otherCommentId.Value.ShouldBeGreaterThan(secondId.Value);
        AddWatermark(projectId, masterId, otherDiscussionId, otherCommentId);

        using (var scope = factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICommentDiscussionRepository>();
            (await repository.GetReadWatermark(discussionId, masterId)).ShouldBe(secondId);
            // Отметки другого пользователя чужие.
            (await repository.GetReadWatermark(discussionId, otherUserId)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task ForumDiscussion_OwnerIsThread()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для владельца темы");
        }

        var threadId = await CreateThreadAsync(masterId, projectId, hideFromUser: true);
        var discussionId = new CommentDiscussionId(GetThreadDiscussionId(threadId));

        using var scope2 = factory.Services.CreateScope();
        var rootGroupId = (await scope2.ServiceProvider.GetRequiredService<IProjectMetadataRepository>()
            .GetProjectMetadata(projectId)).GroupTree.RootGroupId;
        var expectedOwner = new ForumDiscussionOwner(threadId, rootGroupId, IsVisibleToPlayer: false);
        var repository = scope2.ServiceProvider.GetRequiredService<ICommentDiscussionRepository>();

        var discussion = (await repository.GetDiscussionOrDefault(discussionId)).ShouldNotBeNull();
        discussion.Owner.ShouldBe(expectedOwner);
        var comment = discussion.Comments.ShouldHaveSingleItem();
        comment.Author.ShouldBe(masterId);
        comment.Finance.ShouldBeNull();

        (await repository.GetOwnerOrDefault(discussionId)).ShouldBe(expectedOwner);
        (await repository.GetOwnerByCommentOrDefault(comment.Id)).ShouldBe(expectedOwner);
    }

    private Task<ForumThreadIdentification> CreateThreadAsync(UserIdentification masterId, ProjectIdentification projectId, bool hideFromUser)
        => factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IForumService>().CreateThread(
                projectInfo.GroupTree.RootGroupId,
                "Тема для снимка",
                "Первый комментарий в теме",
                hideFromUser,
                emailEverybody: false);
        });

    private int GetClaimDiscussionId(ClaimIdentification claimId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<Claim>()
            .Where(c => c.ClaimId == claimId.ClaimId)
            .Select(c => c.CommentDiscussionId)
            .Single();
    }

    private int GetThreadDiscussionId(ForumThreadIdentification threadId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<ForumThread>()
            .Where(t => t.ForumThreadId == threadId.ThreadId)
            .Select(t => t.CommentDiscussionId)
            .Single();
    }

    private CommentId GetCommentId(CommentDiscussionId discussionId, string text)
    {
        using var scope = factory.Services.CreateScope();
        return new CommentId(scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<Comment>()
            .Where(c => c.CommentDiscussionId == discussionId.Value && c.CommentText.Text.Contents == text)
            .Select(c => c.CommentId)
            .Single());
    }

    private CommentId[] GetCommentIds(CommentDiscussionId discussionId)
    {
        using var scope = factory.Services.CreateScope();
        return [.. scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<Comment>()
            .Where(c => c.CommentDiscussionId == discussionId.Value)
            .Select(c => c.CommentId)
            .ToList()
            .Select(id => new CommentId(id))];
    }

    private void AddWatermark(ProjectIdentification projectId, UserIdentification userId, CommentDiscussionId discussionId, CommentId commentId)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        _ = ctx.Set<ReadCommentWatermark>().Add(new ReadCommentWatermark
        {
            ProjectId = projectId.Value,
            UserId = userId.Value,
            CommentDiscussionId = discussionId.Value,
            CommentId = commentId.Value,
        });
        _ = ctx.SaveChanges();
    }
}
