using System.Net;
using HtmlAgilityPack;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страницы обсуждений — заявки и темы форума — на наполненном дереве комментариев, глазами мастера
/// и игрока (ADR026, шаг 1).
/// </summary>
/// <remarks>
/// <para>
/// В смоук-сиде (<see cref="SmokeProjectFixture"/>) обсуждения почти пустые: нет ответов, комментариев
/// игрока, скрытых от игрока комментариев, неподтверждённой финоперации и вида со стороны игрока.
/// Поэтому нули в снапшоте на этих страницах ничего не доказывают. Здесь у заявки и темы есть всё
/// это сразу, а <c>LazyLoadAssertingHandler</c> на клиентах фабрики сверяет число ленивых загрузок
/// с <c>lazy-loads-baseline.json</c> (см. docs/lazy-loads-baseline.md) — до перевода обсуждений
/// на доменный снимок.
/// </para>
/// <para>
/// Каждый тест сеет свой проект: страница заявки сдвигает отметку прочтения, и порядок тестов
/// не должен влиять на пометку «новое».
/// </para>
/// </remarks>
public class CommentDiscussionPagesScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string NewCommentBadgeTitle = "Новый комментарий";

    [Fact]
    public async Task ClaimAndForumPages_ShowWholeDiscussion_ToMaster()
    {
        var seed = await SeedAsync();
        var master = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), seed.MasterEmail);

        var claimPage = await GetPageAsync(master, seed.ClaimPageUrl);
        var claimText = TextOf(claimPage);
        claimText.ShouldContain(ClaimTexts.MasterRoot);
        claimText.ShouldContain(ClaimTexts.MasterReply);
        claimText.ShouldContain(ClaimTexts.PlayerReply);
        claimText.ShouldContain(ClaimTexts.HiddenFromPlayer);
        claimText.ShouldContain(ClaimTexts.Payment);
        claimText.ShouldContain("Способ оплаты");

        var threadPage = await GetPageAsync(master, seed.ThreadPageUrl);
        var threadText = TextOf(threadPage);
        threadText.ShouldContain(ForumTexts.MasterReply);
        threadText.ShouldContain(ForumTexts.PlayerReply);
        threadText.ShouldContain(ForumTexts.HiddenFromPlayer);

        var listPage = await GetPageAsync(master, seed.ThreadListUrl);
        TextOf(listPage).ShouldContain(ForumTexts.Title);
    }

    [Fact]
    public async Task ClaimAndForumPages_HideConcealedComments_FromPlayer()
    {
        var seed = await SeedAsync();
        var player = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), seed.PlayerEmail);

        // «Моя заявка» отвечает редиректом на страницу заявки — клиент по нему проходит.
        var myClaimPage = await GetPageAsync(player, seed.MyClaimUrl);
        var myClaimText = TextOf(myClaimPage);
        myClaimText.ShouldContain(ClaimTexts.MasterRoot);
        myClaimText.ShouldContain(ClaimTexts.MasterReply);
        myClaimText.ShouldContain(ClaimTexts.PlayerReply);
        myClaimText.ShouldContain(ClaimTexts.Payment);
        myClaimText.ShouldNotContain(ClaimTexts.HiddenFromPlayer);
        // Свой неподтверждённый платёж игрок видит ждущим модерации — модерировать его он не может.
        myClaimText.ShouldContain("Ждет одобрения мастером");

        var threadPage = await GetPageAsync(player, seed.ThreadPageUrl);
        var threadText = TextOf(threadPage);
        threadText.ShouldContain(ForumTexts.MasterReply);
        threadText.ShouldContain(ForumTexts.PlayerReply);
        threadText.ShouldNotContain(ForumTexts.HiddenFromPlayer);

        var listPage = await GetPageAsync(player, seed.ThreadListUrl);
        TextOf(listPage).ShouldContain(ForumTexts.Title);
    }

    /// <summary>
    /// Второй мастер впервые открывает заявку: всё, что написал первый мастер, для него новое —
    /// и корневой комментарий, и ответ на него.
    /// </summary>
    /// <remarks>
    /// ADR026 («Попутно найдено») подозревал, что ответы здесь новыми не бывают: они строятся
    /// отложенным <c>Select</c> (<c>CommentViewModel.ChildComments</c>) уже при рендере, после
    /// <c>UpdateReadCommentWatermark</c> в <c>ClaimController.ShowClaim</c>. Тест это не подтвердил —
    /// ответ помечен новым. Коллекция отметок <c>Discussion.Watermarks</c> загружается ещё до
    /// обновления (проверкой «есть ли непрочитанное»), а отметку <c>ClaimService</c> пишет в другом
    /// транзиентном <c>MyDbContext</c> — граф заявки её не видит. Тест фиксирует поведение, которое
    /// должен сохранить перевод страницы на доменный снимок, где «новое» считается до обновления отметки.
    /// </remarks>
    [Fact]
    public async Task ClaimPage_MarksRootAndReplyAsNew_ForMasterWhoOpensItFirstTime()
    {
        var seed = await SeedAsync();
        var secondMaster = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), seed.SecondMasterEmail);

        var claimPage = await GetPageAsync(secondMaster, seed.ClaimPageUrl);

        IsMarkedNew(claimPage, seed.MasterRootCommentId).ShouldBeTrue("Корневой комментарий должен быть новым");
        IsMarkedNew(claimPage, seed.MasterReplyCommentId).ShouldBeTrue("Ответ на комментарий должен быть новым");

        // Первое открытие сдвинуло отметку прочтения — при повторном ничего нового нет.
        var reopenedPage = await GetPageAsync(secondMaster, seed.ClaimPageUrl);
        IsMarkedNew(reopenedPage, seed.MasterRootCommentId).ShouldBeFalse("Прочитанный комментарий не должен быть новым");
        IsMarkedNew(reopenedPage, seed.MasterReplyCommentId).ShouldBeFalse("Прочитанный ответ не должен быть новым");
    }

    /// <summary>
    /// Помечен ли комментарий значком «новое». Блок комментария (<c>div#comment{id}</c>) содержит
    /// и вложенные ответы, поэтому значок ищем только в собственной таблице комментария.
    /// </summary>
    private static bool IsMarkedNew(HtmlDocument page, int commentId)
    {
        var header = page.DocumentNode.SelectSingleNode($"//div[@id='comment{commentId}']/table")
            ?? throw new ShouldAssertException($"Комментарий {commentId} не найден на странице");
        return WebUtility.HtmlDecode(header.InnerText).Contains(NewCommentBadgeTitle);
    }

    private static async Task<HtmlDocument> GetPageAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, url);
        return await response.AsHtmlDocument();
    }

    private static string TextOf(HtmlDocument page) => WebUtility.HtmlDecode(page.DocumentNode.InnerText);

    private static class ClaimTexts
    {
        public const string MasterRoot = "Мастер: расскажи подробнее о персонаже";
        public const string MasterReply = "Мастер: и про его семью тоже";
        public const string PlayerReply = "Игрок: отвечаю на вопрос мастера";
        public const string HiddenFromPlayer = "Мастерам: игроку пока не говорим";
        public const string Payment = "Отдал наличными";
    }

    private static class ForumTexts
    {
        public const string Title = "Тема для обсуждения игроками";
        public const string First = "Первый комментарий в теме";
        public const string MasterReply = "Мастер: уточнение к первому комментарию";
        public const string PlayerReply = "Игрок: вопрос по теме";
        public const string HiddenFromPlayer = "Мастерам: обсуждаем без игроков";
    }

    private sealed record Seed(
        ProjectIdentification ProjectId,
        ClaimIdentification ClaimId,
        ForumThreadIdentification ThreadId,
        int MasterRootCommentId,
        int MasterReplyCommentId,
        string MasterEmail,
        string SecondMasterEmail,
        string PlayerEmail)
    {
        public string ClaimPageUrl => $"{ProjectId.Value}/claim/{ClaimId.ClaimId}/edit";
        public string MyClaimUrl => $"{ProjectId.Value}/myclaim";
        public string ThreadPageUrl => $"{ProjectId.Value}/forums/{ThreadId.ThreadId}/ViewThread";
        public string ThreadListUrl => $"{ProjectId.Value}/forums";
    }

    /// <summary>
    /// Проект с двумя мастерами и игроком. У принятой заявки игрока — дерево комментариев (комментарий
    /// мастера, ответ мастера на него, ответ игрока), скрытый от игрока комментарий и неподтверждённый
    /// платёж игрока на кассу мастера. У темы форума — ответ мастера, ответ игрока и скрытый комментарий.
    /// Второй мастер ничего не пишет и ничего не открывает: для него вся заявка — новая.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        UserIdentification masterId;
        string masterEmail;
        UserIdentification secondMasterId;
        string secondMasterEmail;
        UserIdentification playerId;
        string playerEmail;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            (secondMasterId, secondMasterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с обсуждениями");
        }

        var (characterId, threadId) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            await sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = secondMasterId,
                Role = new("Второй мастер"),
                Description = new MarkdownString("Читает заявки"),
                IsPublic = false,
                Permissions = [Permission.CanManageClaims],
            });

            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = masterId,
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            });

            var characterId = await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));

            var threadId = await sp.GetRequiredService<IForumService>().CreateThread(
                projectInfo.GroupTree.RootGroupId,
                ForumTexts.Title,
                ForumTexts.First,
                hideFromUser: false,
                emailEverybody: false);
            return (characterId, threadId);
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));

        var claimDiscussionId = GetClaimDiscussionId(claimId);
        var threadDiscussionId = GetThreadDiscussionId(threadId);

        await AddClaimComment(masterId, claimId, parentCommentId: null, ClaimTexts.MasterRoot);
        var masterRootId = GetCommentId(claimDiscussionId, ClaimTexts.MasterRoot);
        await AddClaimComment(masterId, claimId, masterRootId, ClaimTexts.MasterReply);
        var masterReplyId = GetCommentId(claimDiscussionId, ClaimTexts.MasterReply);
        await AddClaimComment(playerId, claimId, masterRootId, ClaimTexts.PlayerReply);
        await AddClaimComment(masterId, claimId, parentCommentId: null, ClaimTexts.HiddenFromPlayer, isVisibleToPlayer: false);

        // Платит сам игрок на кассу мастера — такая операция создаётся неподтверждённой.
        await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IFinanceService>().FeeAcceptedOperation(new FeeAcceptedOperationRequest
            {
                ClaimId = claimId.ClaimId,
                Contents = ClaimTexts.Payment,
                OperationDate = DateTime.UtcNow.Date,
                Money = 500,
                PaymentTypeId = projectInfo.ProjectFinanceSettings.GetCashPaymentType(masterId)!.PaymentTypeId,
            });
        });
        GetFinanceOperationState(claimId).ShouldBe(FinanceOperationState.Proposed, "Платёж игрока должен ждать модерации");

        var forumFirstId = GetCommentId(threadDiscussionId, ForumTexts.First);
        await AddForumComment(masterId, threadId, forumFirstId, ForumTexts.MasterReply);
        await AddForumComment(playerId, threadId, forumFirstId, ForumTexts.PlayerReply);
        await AddForumComment(masterId, threadId, parentCommentId: null, ForumTexts.HiddenFromPlayer, isVisibleToPlayer: false);

        return new Seed(projectId, claimId, threadId, masterRootId, masterReplyId, masterEmail, secondMasterEmail, playerEmail);
    }

    private Task AddClaimComment(
        UserIdentification authorId,
        ClaimIdentification claimId,
        int? parentCommentId,
        string text,
        bool isVisibleToPlayer = true)
        => factory.Services.RunAsAsync(authorId, sp =>
            sp.GetRequiredService<IClaimService>().AddComment(claimId, parentCommentId, isVisibleToPlayer, text));

    private Task AddForumComment(
        UserIdentification authorId,
        ForumThreadIdentification threadId,
        int? parentCommentId,
        string text,
        bool isVisibleToPlayer = true)
        => factory.Services.RunAsAsync(authorId, sp =>
            sp.GetRequiredService<IForumService>().AddComment(threadId, parentCommentId, isVisibleToPlayer, text));

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

    private int GetCommentId(int discussionId, string text)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<Comment>()
            .Where(c => c.CommentDiscussionId == discussionId && c.CommentText.Text.Contents == text)
            .Select(c => c.CommentId)
            .Single();
    }

    private FinanceOperationState GetFinanceOperationState(ClaimIdentification claimId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
            .Where(fo => fo.ProjectId == claimId.ProjectId.Value && fo.ClaimId == claimId.ClaimId)
            .Select(fo => fo.State)
            .Single();
    }
}
