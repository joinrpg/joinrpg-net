using System.Data.Entity.SqlServer;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Interfaces;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class CharacterGroupRepository(
    MyDbContext ctx,
    IProjectMetadataRepository projectMetadataRepository
) : ICharacterGroupRepository
{
    public async Task<CharacterGroupFullInfo?> GetCharacterGroupFullInfo(CharacterGroupIdentification id)
    {
        var results = await GetCharacterGroupsFullInfo([id]);
        return results.Count == 0 ? null : results[0];
    }

    public async Task<IReadOnlyList<CharacterGroupFullInfo>> GetCharacterGroupsFullInfo(
        IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        if (groupIds.Count == 0)
        {
            return [];
        }

        var projectId = groupIds.EnsureSameProject().First().ProjectId;
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var intIds = groupIds.Select(g => g.CharacterGroupId).ToList();

        var rows = await ctx.Set<CharacterGroup>()
            .AsExpandable()
            .Where(cg => cg.ProjectId == projectId.Value && intIds.Contains(cg.CharacterGroupId))
            .Select(cg => new
            {
                cg.CharacterGroupId,
                cg.Description.Contents,
                cg.CreatedAt,
                cg.UpdatedAt,
                CreatedById = (int?)cg.CreatedBy.UserId,
                CreatedPreffered = cg.CreatedBy.PrefferedName,
                CreatedBorn = cg.CreatedBy.BornName,
                CreatedSur = cg.CreatedBy.SurName,
                CreatedFather = cg.CreatedBy.FatherName,
                CreatedEmail = cg.CreatedBy.Email,
                UpdatedById = (int?)cg.UpdatedBy.UserId,
                UpdatedPreffered = cg.UpdatedBy.PrefferedName,
                UpdatedBorn = cg.UpdatedBy.BornName,
                UpdatedSur = cg.UpdatedBy.SurName,
                UpdatedFather = cg.UpdatedBy.FatherName,
                UpdatedEmail = cg.UpdatedBy.Email,
                CharacterCount = ctx.Set<Character>().Count(c =>
                    c.ProjectId == projectId.Value &&
                    SqlFunctions.CharIndex(
                        "," + SqlFunctions.StringConvert((double?)cg.CharacterGroupId).Trim() + ",",
                        "," + c.ParentGroupsImpl.ListIds + ",") > 0),
            })
            .ToListAsync();

        var result = new List<CharacterGroupFullInfo>(rows.Count);
        foreach (var row in rows)
        {
            var id = new CharacterGroupIdentification(projectId, row.CharacterGroupId);
            if (!projectInfo.Groups.TryGetValue(id, out var groupInfo))
            {
                continue;
            }

            MarkdownString? description = row.Contents is null ? null : new MarkdownString(row.Contents);
            var marks = new CreateUpdateMarksInfo(
                row.CreatedAt,
                BuildUserInfo(row.CreatedById, row.CreatedPreffered, row.CreatedBorn, row.CreatedSur, row.CreatedFather, row.CreatedEmail),
                row.UpdatedAt,
                BuildUserInfo(row.UpdatedById, row.UpdatedPreffered, row.UpdatedBorn, row.UpdatedSur, row.UpdatedFather, row.UpdatedEmail));

            result.Add(new CharacterGroupFullInfo(groupInfo, row.CharacterCount, description, marks));
        }
        return result;
    }

    private static UserInfoHeader? BuildUserInfo(int? userId, string? preffered, string? born, string? sur, string? father, string? email)
    {
        if (userId is null || email is null)
        {
            return null;
        }

        return new UserInfoHeader(
            new UserIdentification(userId.Value),
            new UserDisplayName(
                new UserFullName(
                    PrefferedName.FromOptional(preffered),
                    BornName.FromOptional(born),
                    SurName.FromOptional(sur),
                    FatherName.FromOptional(father)),
                new Email(email)));
    }
}
