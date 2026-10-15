using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.WebPortal.Managers.Drp;
using Microsoft.Extensions.Options;

namespace JoinRpg.WebPortal.Managers.Test.Drp;

public class CommentRestoreViewServiceTest
{
    private static readonly DateTimeOffset BackupTakenAt = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);
    private const string CharacterName = "Герой";

    private readonly MockedProject mock = new();
    private readonly FakeClaimRestoreService restoreService = new();
    private readonly FakeNotificationRepository notifications = new();
    private readonly FakeLookupRepository lookup = new();

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private UserIdentification Player => new(mock.Player.UserId);

    private UserIdentification Master => new(mock.Master.UserId);

    private ClaimIdentification Claim(int id) => new(ProjectId, id);

    private CommentRestoreViewService CreateService(int userId) => new(
        new FakeProjectMetadataRepository(mock),
        new FakeCurrentUserAccessor(userId),
        notifications,
        new FakeUserRepository(mock),
        restoreService,
        lookup,
        Options.Create(new DrpOptions { BackupTakenAt = BackupTakenAt, LostAt = LostAt }));

    private void BlockProject()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
    }

    /// <summary>Комментарий мастера, как его пишет <c>ClaimNotificationTextBuilder</c>: по строке на получателя.</summary>
    private void MasterComment(ClaimIdentification claimId, int commentId, DateTimeOffset at, string text, params User[] recipients)
    {
        var header = $"Игра: {CharacterName}, игрок {mock.Player.PrefferedName}";
        foreach (var recipient in recipients)
        {
            var body = $"Добрый день, {recipient.PrefferedName}\nЗаявка {CharacterName} игрока {mock.Player.PrefferedName} откомментирована мастером {mock.Master.PrefferedName}\n\n{text}";
            notifications.AddClaimNotification(new NotificationMessageForRecipient(
                new MarkdownString(body), Master, header, new UserIdentification(recipient.UserId),
                new ClaimCommentIdentification(claimId, commentId), at));
        }
    }

    private void ClaimInDb(int claimId) => lookup.Claims.Add(new ClaimForCommentRestore(Claim(claimId), Player, CharacterName));

    [Fact]
    public async Task GetPlan_ProjectNotBlocked_ExplainsWithoutReadingNotifications()
    {
        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.ProjectNotBlocked);
        model.Rows.ShouldBeEmpty();
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_NotMaster_NoAccess()
    {
        BlockProject();

        var model = await CreateService(mock.Player.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.NoAccess);
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_BuildsRowsPerClaim()
    {
        BlockProject();
        ClaimInDb(5);
        MasterComment(Claim(5), 555, BackupTakenAt.AddDays(1), "Ждём **взнос**", mock.Player);
        MasterComment(Claim(5), 556, BackupTakenAt.AddDays(2), "Уже есть", mock.Player);
        MasterComment(Claim(6), 600, BackupTakenAt.AddDays(3), "В потерянную заявку", mock.Player);
        lookup.Comments.Add(new ClaimCommentForRestore(556, Claim(5), Master, BackupTakenAt.AddDays(2), false, true, "Уже есть"));

        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBeNull();
        notifications.GetClaimNotificationsForProjectCalls.ShouldHaveSingleItem().ShouldBe((ProjectId, LostAt));
        lookup.NotifiedCommentIds.ShouldBe([555, 556, 600], ignoreOrder: true);
        lookup.TextsSince.ShouldBe([BackupTakenAt.AddMinutes(-1)]);
        model.Rows.Select(r => r.ClaimId.ClaimId).ShouldBe([5, 6]);

        var row = model.Rows[0];
        row.ClaimExists.ShouldBeTrue();
        row.CharacterName.ShouldBe(CharacterName);
        row.PlayerName.ShouldNotBeNull().ShouldContain(mock.Player.PrefferedName!);
        row.SkippedExisting.ShouldBe(1);
        row.Warnings.ShouldBeEmpty();
        var comment = row.Comments.ShouldHaveSingleItem();
        comment.CommentId.ShouldBe(555);
        comment.CreatedAt.ShouldBe(BackupTakenAt.AddDays(1));
        comment.AuthorName.ShouldContain(mock.Master.PrefferedName!);
        comment.IsCommentByPlayer.ShouldBeFalse();
        comment.IsVisibleToPlayer.ShouldBeTrue();
        comment.ActionName.ShouldBeNull();
        comment.TextHtml.ShouldContain("<strong>взнос</strong>");

        var lost = model.Rows[1];
        lost.ClaimExists.ShouldBeFalse();
        lost.CharacterName.ShouldBeNull();
        lost.Comments.ShouldBeEmpty();
        lost.Warnings.ShouldBe([CommentRestoreWarningView.ClaimNotFound]);
    }

    [Fact]
    public async Task GetPlan_IdTakenElsewhere_Conflict()
    {
        BlockProject();
        ClaimInDb(5);
        MasterComment(Claim(5), 555, BackupTakenAt.AddDays(1), "Текст", mock.Player);
        lookup.Taken.Add(555);

        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        var row = model.Rows.ShouldHaveSingleItem();
        row.Comments.ShouldBeEmpty();
        row.Conflicts.ShouldBe([555]);
        row.Warnings.ShouldBe([CommentRestoreWarningView.CommentIdConflict]);
    }

    [Fact]
    public async Task Apply_RecomputesPlanAndSendsOnlyThisClaim()
    {
        BlockProject();
        ClaimInDb(5);
        ClaimInDb(7);
        MasterComment(Claim(5), 555, BackupTakenAt.AddDays(1), "Видимый", mock.Player);
        MasterComment(Claim(5), 556, BackupTakenAt.AddDays(2), "Скрытый", new User { UserId = 99, PrefferedName = "Другой мастер" });
        MasterComment(Claim(7), 700, BackupTakenAt.AddDays(3), "Другая заявка", mock.Player);
        restoreService.Result = new RestoredCommentsResult(2, 0, []);

        var result = await CreateService(mock.Master.UserId).Apply(Claim(5));

        result.ShouldBe(new CommentRestoreApplyResult(2, 0, []));
        // Читается только эта заявка — и уведомления, и база.
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
        notifications.GetClaimNotificationsForClaimCalls.ShouldHaveSingleItem().ShouldBe((Claim(5), LostAt));
        lookup.ClaimCalls.ShouldBe([Claim(5)]);
        lookup.NotifiedCommentIds.ShouldBe([555, 556], ignoreOrder: true);
        var (claimId, comments) = restoreService.Calls.ShouldHaveSingleItem();
        claimId.ShouldBe(Claim(5));
        comments.ShouldBe([
            new RestoredClaimComment(555, Master, false, true, null, BackupTakenAt.AddDays(1), "Видимый"),
            new RestoredClaimComment(556, Master, false, false, null, BackupTakenAt.AddDays(2), "Скрытый"),
        ]);
    }

    [Fact]
    public async Task Apply_IdTakenByOtherClaimOfProject_NotSent()
    {
        BlockProject();
        ClaimInDb(5);
        ClaimInDb(7);
        MasterComment(Claim(5), 555, BackupTakenAt.AddDays(1), "Занятый номер", mock.Player);
        MasterComment(Claim(5), 556, BackupTakenAt.AddDays(2), "Свободный номер", mock.Player);
        lookup.Comments.Add(new ClaimCommentForRestore(555, Claim(7), Master, BackupTakenAt.AddDays(1), false, true, "Чужой"));

        _ = await CreateService(mock.Master.UserId).Apply(Claim(5));

        restoreService.Calls.ShouldHaveSingleItem().Comments.Select(c => c.CommentId).ShouldBe([556]);
    }

    [Fact]
    public async Task Apply_ClaimNotInDb_Refuses()
    {
        BlockProject();
        MasterComment(Claim(6), 600, BackupTakenAt.AddDays(1), "Текст", mock.Player);

        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).Apply(Claim(6)));

        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_NothingToRestore_DoesNotCallService()
    {
        BlockProject();
        ClaimInDb(5);
        MasterComment(Claim(5), 555, BackupTakenAt.AddDays(1), "Текст", mock.Player);
        lookup.Comments.Add(new ClaimCommentForRestore(555, Claim(5), Master, BackupTakenAt.AddDays(1), false, true, "Текст"));

        var result = await CreateService(mock.Master.UserId).Apply(Claim(5));

        result.ShouldBe(new CommentRestoreApplyResult(0, 0, []));
        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_ProjectNotBlocked_RefusesWithoutReadingNotifications()
    {
        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).Apply(Claim(5)));

        notifications.GetClaimNotificationsForClaimCalls.ShouldBeEmpty();
    }

    /// <summary>Модели ездят между сервером и островом по JSON (у мок-проекта id нулевой, поэтому свои id).</summary>
    [Fact]
    public void Models_SurviveJson()
    {
        var projectId = new ProjectIdentification(1);
        var comment = new CommentRestoreCommentView(555, BackupTakenAt, "Мастер", false, true, true, "Заявка принята", "<p>текст</p>");
        var row = new CommentRestoreRowViewModel(
            new ClaimIdentification(projectId, 5), true, CharacterName, "Игрок", [comment], 1, [7], 2, 3,
            [CommentRestoreWarningView.CommentIdConflict]);
        var plan = new CommentRestorePlanViewModel(projectId, null, [row]);

        var restored = JsonRoundTrip.Ensure(plan);
        var restoredRow = restored.Rows.ShouldHaveSingleItem();
        restoredRow.ClaimId.ShouldBe(row.ClaimId);
        restoredRow.Comments.ShouldHaveSingleItem().ShouldBe(comment);
        restoredRow.Conflicts.ShouldBe([7]);
        restoredRow.Warnings.ShouldBe([CommentRestoreWarningView.CommentIdConflict]);

        var result = new CommentRestoreApplyResult(1, 2, [3]);
        JsonRoundTrip.Ensure(result).Conflicts.ShouldBe([3]);
    }

    private sealed class FakeLookupRepository : IProjectRestoreLookupRepository
    {
        public List<ClaimForCommentRestore> Claims { get; } = [];
        public List<ClaimCommentForRestore> Comments { get; } = [];
        public HashSet<int> Taken { get; } = [];
        public List<int> NotifiedCommentIds { get; } = [];
        public List<ClaimIdentification> ClaimCalls { get; } = [];
        public List<DateTimeOffset> TextsSince { get; } = [];

        public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(
            ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
            => Lookup(c => c.ProjectId == projectId, notifiedCommentIds, textsSince);

        public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(
            ClaimIdentification claimId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
        {
            ClaimCalls.Add(claimId);
            return Lookup(c => c == claimId, notifiedCommentIds, textsSince);
        }

        /// <summary>Как в базе: занят — и номер из <see cref="Taken"/>, и номер комментария не из выбранных заявок.</summary>
        private Task<ClaimCommentsRestoreLookup> Lookup(Func<ClaimIdentification, bool> claimFilter, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
        {
            NotifiedCommentIds.AddRange(notifiedCommentIds);
            TextsSince.Add(textsSince);
            List<ClaimCommentForRestore> comments = [.. Comments.Where(c => claimFilter(c.ClaimId))];
            return Task.FromResult(new ClaimCommentsRestoreLookup(
                [.. Claims.Where(c => claimFilter(c.ClaimId))],
                comments,
                [.. notifiedCommentIds.Where(id => Taken.Contains(id)
                    || (Comments.Any(c => c.CommentId == id) && !comments.Any(c => c.CommentId == id)))]));
        }

        public Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsTakenByOtherProjects(IReadOnlyCollection<CharacterGroupIdentification> groupIds)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<CharacterIdentification>> GetCharactersTakenByOtherProjects(IReadOnlyCollection<CharacterIdentification> characterIds)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<string, UserIdentification>> FindUsersByEmails(IReadOnlyCollection<string> emails)
            => throw new NotSupportedException();

        public Task<ClaimFinanceRestoreLookup> GetClaimFinanceForRestore(ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ProjectIdentification>> GetBlockedProjects() => throw new NotSupportedException();
    }

    private sealed class FakeClaimRestoreService : IClaimRestoreService
    {
        public List<(ClaimIdentification ClaimId, IReadOnlyList<RestoredClaimComment> Comments)> Calls { get; } = [];

        public RestoredCommentsResult Result { get; set; } = new(0, 0, []);

        public Task<RestoredCommentsResult> RestoreClaimComments(ClaimIdentification claimId, IReadOnlyList<RestoredClaimComment> comments)
        {
            Calls.Add((claimId, comments));
            return Task.FromResult(Result);
        }

        public Task ApplyRestoredState(ClaimIdentification claimId, RestoredClaimTarget target) => throw new NotSupportedException();

        public Task<CharacterIdentification> RestoreLostClaim(LostClaimToRestore claim) => throw new NotSupportedException();

        public Task<RestoredFinanceResult> RestoreFinanceOperations(ClaimIdentification claimId,
            IReadOnlyList<RestoredFinanceOperation> operations, IReadOnlyList<RestoredFinanceModeration> moderations)
            => throw new NotSupportedException();
    }
}
