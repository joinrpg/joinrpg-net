using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.Dal.Impl.Repositories;

internal class CharacterGroupRepository(
    MyDbContext ctx,
    IProjectMetadataRepository projectMetadataRepository
) : ICharacterGroupRepository
{
    public async Task<CharacterGroupFullInfo?> GetCharacterGroupFullInfo(CharacterGroupIdentification id)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
        if (!projectInfo.Groups.TryGetValue(id, out var groupInfo))
        {
            return null;
        }

        var data = await ctx.Set<CharacterGroup>()
            .Where(cg => cg.CharacterGroupId == id.CharacterGroupId && cg.ProjectId == id.ProjectId)
            .Select(cg => new
            {
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
            })
            .SingleOrDefaultAsync();

        if (data is null)
        {
            return null;
        }

        var charactersCount = await ctx.Set<Character>()
            .Where(CharacterPredicates.ByGroup([id]))
            .CountAsync();

        MarkdownString? description = data.Contents is null ? null : new MarkdownString(data.Contents);

        var marks = new CreateUpdateMarksInfo(
            data.CreatedAt,
            BuildUserInfo(data.CreatedById, data.CreatedPreffered, data.CreatedBorn, data.CreatedSur, data.CreatedFather, data.CreatedEmail),
            data.UpdatedAt,
            BuildUserInfo(data.UpdatedById, data.UpdatedPreffered, data.UpdatedBorn, data.UpdatedSur, data.UpdatedFather, data.UpdatedEmail));

        return new CharacterGroupFullInfo(
            groupInfo.Id, groupInfo.Name, groupInfo.IsRoot, groupInfo.IsActive,
            groupInfo.IsPublic, groupInfo.IsSpecial, groupInfo.IsIntresting,
            groupInfo.DirectChildGroupIds, groupInfo.ChildCharactersOrdering,
            groupInfo.DirectParentGroupIds, groupInfo.AllChildGroups, groupInfo.AllParentGroups,
            charactersCount, description, marks);
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
