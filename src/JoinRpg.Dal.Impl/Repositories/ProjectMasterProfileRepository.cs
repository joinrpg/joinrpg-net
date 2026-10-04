namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectMasterProfileRepository(MyDbContext ctx) : IProjectMasterProfileRepository
{
    public Task<IReadOnlyCollection<ProjectMasterProfileDto>> GetMasterProfiles(ProjectIdentification projectId)
        => LoadProfiles(acl => acl.ProjectId == projectId.Value && acl.Status == ProjectAclStatus.Active);

    public async Task<ProjectMasterProfileDto?> GetMasterProfile(ProjectIdentification projectId, UserIdentification userId)
        => (await LoadProfiles(acl => acl.ProjectId == projectId.Value && acl.UserId == userId.Value)).SingleOrDefault();

    private async Task<IReadOnlyCollection<ProjectMasterProfileDto>> LoadProfiles(Expression<Func<ProjectAcl, bool>> predicate)
    {
        var rows = await ctx.Set<ProjectAcl>()
            .Where(predicate)
            .Select(acl => new { acl.UserId, acl.Role, acl.Description.Contents })
            .ToListAsync();

        return [.. rows.Select(r => new ProjectMasterProfileDto(
            new UserIdentification(r.UserId),
            new MasterRoleTitle(r.Role),
            MarkdownString.FromOptional(r.Contents)))];
    }
}
