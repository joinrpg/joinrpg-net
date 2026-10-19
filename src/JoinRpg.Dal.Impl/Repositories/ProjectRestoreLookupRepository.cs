using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectRestoreLookupRepository(MyDbContext ctx) : IProjectRestoreLookupRepository
{
    public async Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsTakenByOtherProjects(IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        if (groupIds.Count == 0)
        {
            return [];
        }
        var projectId = groupIds.EnsureSameProject().First().ProjectId;
        var ids = groupIds.Select(g => g.CharacterGroupId).Distinct().ToList();
        var taken = await ctx.Set<CharacterGroup>()
            .Where(g => g.ProjectId != projectId.Value && ids.Contains(g.CharacterGroupId))
            .Select(g => g.CharacterGroupId)
            .ToListAsync();
        return [.. taken.Distinct().Select(id => new CharacterGroupIdentification(projectId, id))];
    }

    public async Task<IReadOnlyCollection<CharacterIdentification>> GetCharactersTakenByOtherProjects(IReadOnlyCollection<CharacterIdentification> characterIds)
    {
        if (characterIds.Count == 0)
        {
            return [];
        }
        var projectId = characterIds.EnsureSameProject().First().ProjectId;
        var ids = characterIds.Select(c => c.CharacterId).Distinct().ToList();
        var taken = await ctx.Set<Character>()
            .Where(c => c.ProjectId != projectId.Value && ids.Contains(c.CharacterId))
            .Select(c => c.CharacterId)
            .ToListAsync();
        return [.. taken.Distinct().Select(id => new CharacterIdentification(projectId, id))];
    }

    public async Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsCreatedAfter(IReadOnlyCollection<CharacterGroupIdentification> groupIds, DateTimeOffset since)
    {
        if (groupIds.Count == 0)
        {
            return [];
        }
        var projectId = groupIds.EnsureSameProject().First().ProjectId;
        var ids = groupIds.Select(g => g.CharacterGroupId).Distinct().ToList();
        var sinceUtc = since.UtcDateTime;
        var created = await ctx.Set<CharacterGroup>()
            .Where(g => g.ProjectId == projectId.Value && ids.Contains(g.CharacterGroupId) && g.CreatedAt > sinceUtc)
            .Select(g => g.CharacterGroupId)
            .ToListAsync();
        return [.. created.Select(id => new CharacterGroupIdentification(projectId, id))];
    }

    public async Task<IReadOnlyDictionary<string, UserIdentification>> FindUsersByEmails(IReadOnlyCollection<string> emails)
    {
        var normalized = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
        var result = new Dictionary<string, UserIdentification>(StringComparer.OrdinalIgnoreCase);
        if (normalized.Count == 0)
        {
            return result;
        }
        // EF6 не поддерживает StringComparison в LINQ; .ToUpper() транслируется в SQL UPPER() — как в FindByEmail.
        var rows = await ctx.Set<User>()
            .Where(u => normalized.Contains(u.Email.ToUpper()))
            .Select(u => new { u.UserId, u.Email })
            .ToListAsync();
        foreach (var row in rows.OrderBy(r => r.UserId))
        {
            _ = result.TryAdd(row.Email.Trim(), new UserIdentification(row.UserId));
        }
        return result;
    }

    public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(
        ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
        => GetClaimCommentsForRestore(projectId, claimId: null, notifiedCommentIds, textsSince);

    public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(
        ClaimIdentification claimId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
        => GetClaimCommentsForRestore(claimId.ProjectId, claimId.ClaimId, notifiedCommentIds, textsSince);

    public async Task<ClaimFinanceRestoreLookup> GetClaimFinanceForRestore(ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds)
    {
        var projectIntId = projectId.Value;
        var claims = ctx.Set<Claim>().Where(c => c.ProjectId == projectIntId);

        var claimIds = await claims.Select(c => c.ClaimId).ToListAsync();

        var operationRows = await ctx.Set<FinanceOperation>()
            .Where(f => f.ProjectId == projectIntId)
            .Select(f => new { f.CommentId, f.ClaimId, f.OperationType, f.State, f.MoneyAmount, f.Created })
            .ToListAsync();

        // Обсуждение заявки — по CommentDiscussionId, как в GetClaimCommentsForRestore.
        var commentRows = await (
            from claim in claims
            join comment in ctx.Set<Comment>() on claim.CommentDiscussionId equals comment.CommentDiscussionId
            where comment.ExtraAction == CommentExtraAction.PaidFee
                || comment.ExtraAction == CommentExtraAction.RefundFee
                || comment.ExtraAction == CommentExtraAction.ApproveFinance
                || comment.ExtraAction == CommentExtraAction.RejectFinance
            select new { claim.ClaimId, comment.CommentId, comment.AuthorUserId, comment.CreatedAt, comment.ExtraAction })
            .ToListAsync();

        // Свои операции и финансовые комментарии заняты «правильно» — в базу спрашиваем только про остальные id.
        var ownIds = operationRows.Select(f => f.CommentId).Concat(commentRows.Select(c => c.CommentId)).ToHashSet();
        // Пачками: EF6 разворачивает Contains в IN со всеми значениями — на тысячах id запрос раздувается.
        var taken = new List<int>();
        foreach (var chunk in notifiedCommentIds.Where(id => !ownIds.Contains(id)).Distinct().Chunk(1000))
        {
            taken.AddRange(await ctx.Set<Comment>()
                .Where(c => chunk.Contains(c.CommentId))
                .Select(c => c.CommentId)
                .ToListAsync());
        }

        var paymentTypeRows = await ctx.Set<PaymentType>()
            .Where(p => p.ProjectId == projectIntId)
            .Select(p => new { p.PaymentTypeId, p.TypeKind, p.UserId, p.IsActive, p.Name })
            .ToListAsync();

        var masterRows = await ctx.Set<ProjectAcl>()
            .Where(a => a.ProjectId == projectIntId && a.CanManageMoney && a.Status == ProjectAclStatus.Active)
            .Select(a => new { a.UserId, a.IsOwner })
            .ToListAsync();

        // EF6 отдаёт DateTime с Kind=Unspecified, а хранится UTC.
        static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

        return new ClaimFinanceRestoreLookup(
            [.. claimIds.Select(id => new ClaimIdentification(projectId, id))],
            [.. operationRows.Select(f => new ClaimFinanceOperationForRestore(
                f.CommentId,
                new ClaimIdentification(projectId, f.ClaimId),
                f.OperationType,
                f.State,
                f.MoneyAmount,
                Utc(f.Created)))],
            [.. commentRows.Select(c => new ClaimFinanceCommentForRestore(
                c.CommentId,
                new ClaimIdentification(projectId, c.ClaimId),
                new UserIdentification(c.AuthorUserId),
                Utc(c.CreatedAt),
                c.ExtraAction!.Value))],
            taken,
            [.. paymentTypeRows.Select(p => new PaymentTypeForRestore(
                new PaymentTypeIdentification(projectId, p.PaymentTypeId),
                p.TypeKind,
                new UserIdentification(p.UserId),
                p.IsActive,
                p.Name))],
            [.. masterRows.Select(a => new MoneyMasterForRestore(new UserIdentification(a.UserId), a.IsOwner))]);
    }

    public async Task<IReadOnlyDictionary<ClaimIdentification, DateTimeOffset>> GetClaimsCommentedNotByRestoreSince(ProjectIdentification projectId, DateTimeOffset since)
    {
        var projectIntId = projectId.Value;
        var sinceUtc = since.UtcDateTime;

        // Обсуждение заявки — по CommentDiscussionId, как в GetClaimCommentsForRestore.
        var rows = await (
            from claim in ctx.Set<Claim>()
            where claim.ProjectId == projectIntId
            join comment in ctx.Set<Comment>() on claim.CommentDiscussionId equals comment.CommentDiscussionId
            where comment.CreatedAt > sinceUtc
                && (comment.ExtraAction == null
                    || (comment.ExtraAction != CommentExtraAction.RestoredFromNotifications
                        && comment.ExtraAction != CommentExtraAction.RestoredFromExport))
            group comment by claim.ClaimId into g
            select new { ClaimId = g.Key, LastAt = g.Max(c => c.CreatedAt) })
            .ToListAsync();

        // EF6 отдаёт DateTime с Kind=Unspecified, а хранится UTC.
        return rows.ToDictionary(
            r => new ClaimIdentification(projectId, r.ClaimId),
            r => new DateTimeOffset(DateTime.SpecifyKind(r.LastAt, DateTimeKind.Utc)));
    }

    public async Task<IReadOnlyList<ProjectIdentification>> GetBlockedProjects()
    {
        var ids = await ctx.Set<Project>()
            .Where(ProjectPredicates.Status(ProjectLifecycleStatus.Blocked))
            .Select(p => p.ProjectId)
            .OrderBy(id => id)
            .ToListAsync();
        return [.. ids.Select(id => new ProjectIdentification(id))];
    }

    private async Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(
        ProjectIdentification projectId, int? claimId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
    {
        var projectIntId = projectId.Value;
        var textsSinceUtc = textsSince.UtcDateTime;

        // Фильтр по заявке — условным Where: EF6 не сокращает «claimId == null || …» в дереве выражений.
        var claims = ctx.Set<Claim>().Where(c => c.ProjectId == projectIntId);
        if (claimId is int claimIntId)
        {
            claims = claims.Where(c => c.ClaimId == claimIntId);
        }

        var claimRows = await claims
            .Select(c => new { c.ClaimId, c.PlayerUserId, c.Character.CharacterName })
            .ToListAsync();

        // Обсуждение заявки — по CommentDiscussionId. Текст — явным LEFT JOIN: связь объявлена обязательной,
        // и на то, что EF6 по навигации всё же сделает внешнее соединение, полагаться не хочется — комментарий
        // без строки CommentTexts пропал бы из выборки, а его id из уведомления сочли бы занятым чужим
        // (проверяет RestoredClaimCommentsScenario). Тексты нужны только для сопоставления комментариев без id
        // с событиями после бэкапа — старые не тянем.
        var commentRows = await (
            from claim in claims
            join comment in ctx.Set<Comment>() on claim.CommentDiscussionId equals comment.CommentDiscussionId
            join commentText in ctx.Set<CommentText>() on comment.CommentId equals commentText.CommentId into texts
            from commentText in texts.DefaultIfEmpty()
            select new
            {
                claim.ClaimId,
                comment.CommentId,
                comment.AuthorUserId,
                comment.CreatedAt,
                comment.IsCommentByPlayer,
                comment.IsVisibleToPlayer,
                Text = comment.CreatedAt >= textsSinceUtc ? commentText.Text.Contents : null,
            })
            .ToListAsync();

        // Свои комментарии заняты «правильно» — в базу спрашиваем только про остальные id.
        var ownIds = commentRows.Select(c => c.CommentId).ToHashSet();
        var candidates = notifiedCommentIds.Where(id => !ownIds.Contains(id)).Distinct().ToList();
        List<int> taken = candidates.Count == 0
            ? []
            : await ctx.Set<Comment>()
                .Where(c => candidates.Contains(c.CommentId))
                .Select(c => c.CommentId)
                .ToListAsync();

        return new ClaimCommentsRestoreLookup(
            [.. claimRows.Select(c => new ClaimForCommentRestore(
                new ClaimIdentification(projectId, c.ClaimId), new UserIdentification(c.PlayerUserId), c.CharacterName))],
            [.. commentRows.Select(c => new ClaimCommentForRestore(
                c.CommentId,
                new ClaimIdentification(projectId, c.ClaimId),
                new UserIdentification(c.AuthorUserId),
                // EF6 отдаёт DateTime с Kind=Unspecified, а хранится UTC.
                new DateTimeOffset(DateTime.SpecifyKind(c.CreatedAt, DateTimeKind.Utc)),
                c.IsCommentByPlayer,
                c.IsVisibleToPlayer,
                c.Text))],
            taken);
    }
}
