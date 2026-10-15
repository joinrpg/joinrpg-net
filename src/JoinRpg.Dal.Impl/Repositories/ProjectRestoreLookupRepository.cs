using JoinRpg.DomainTypes.Interfaces;

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
