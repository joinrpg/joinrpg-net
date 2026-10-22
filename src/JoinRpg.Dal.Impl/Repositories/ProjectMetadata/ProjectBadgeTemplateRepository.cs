using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using ProjectBadgeTemplate = JoinRpg.DomainTypes.ProjectMetadata.Badges.ProjectBadgeTemplate;

namespace JoinRpg.Dal.Impl.Repositories.ProjectMetadata;

internal class ProjectBadgeTemplateRepository(MyDbContext ctx) : IProjectBadgeTemplateRepository
{
    public async Task<IReadOnlyCollection<ProjectBadgeTemplate>> GetForProjectAsync(ProjectIdentification projectId)
    {
        var entities = await ctx.Set<DataModel.ProjectBadgeTemplate>()
            .Where(t => t.ProjectId == projectId.Value)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return [.. entities.Select(ToDomain)];
    }

    public async Task<ProjectBadgeTemplate?> GetByIdAsync(ProjectBadgeTemplateIdentification id)
    {
        var entity = await ctx.Set<DataModel.ProjectBadgeTemplate>()
            .Where(t => t.ProjectId == id.ProjectId.Value && t.ProjectBadgeTemplateId == id.ProjectBadgeTemplateId)
            .FirstOrDefaultAsync();

        return entity == null ? null : ToDomain(entity);
    }

    internal static ProjectBadgeTemplate ToDomain(DataModel.ProjectBadgeTemplate entity)
    {
        return new ProjectBadgeTemplate(
            Id: new ProjectBadgeTemplateIdentification(new ProjectIdentification(entity.ProjectId), entity.ProjectBadgeTemplateId),
            Name: entity.Name,
            Layout: new BadgeLayout(
                WidthMm: entity.WidthMm,
                HeightMm: entity.HeightMm,
                BackgroundUrl: entity.BackgroundUrl,
                Lines: BadgeLinesJson.Deserialize(entity.LinesJson)),
            IsActive: entity.IsActive);
    }
}
