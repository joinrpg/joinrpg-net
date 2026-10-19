using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Выборки для восстановления поверх <see cref="MockedProject"/>. Сервисам восстановления из этого
/// проекта нужны только даты создания групп; остальное не поддержано.
/// </summary>
internal sealed class FakeProjectRestoreLookupRepository(MockedProject mock) : IProjectRestoreLookupRepository
{
    public Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsCreatedAfter(IReadOnlyCollection<CharacterGroupIdentification> groupIds, DateTimeOffset since)
        => Task.FromResult<IReadOnlyCollection<CharacterGroupIdentification>>(
            [.. groupIds.Where(id => mock.Project.CharacterGroups.Any(g => g.CharacterGroupId == id.CharacterGroupId && g.CreatedAt > since.UtcDateTime))]);

    public Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsTakenByOtherProjects(IReadOnlyCollection<CharacterGroupIdentification> groupIds)
        => throw new NotSupportedException();

    public Task<IReadOnlyCollection<CharacterIdentification>> GetCharactersTakenByOtherProjects(IReadOnlyCollection<CharacterIdentification> characterIds)
        => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<string, UserIdentification>> FindUsersByEmails(IReadOnlyCollection<string> emails)
        => throw new NotSupportedException();

    public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
        => throw new NotSupportedException();

    public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(ClaimIdentification claimId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
        => throw new NotSupportedException();

    public Task<ClaimFinanceRestoreLookup> GetClaimFinanceForRestore(ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds)
        => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<ClaimIdentification, DateTimeOffset>> GetClaimsCommentedNotByRestoreSince(ProjectIdentification projectId, DateTimeOffset since)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<ProjectIdentification>> GetBlockedProjects()
        => throw new NotSupportedException();
}
