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
}
