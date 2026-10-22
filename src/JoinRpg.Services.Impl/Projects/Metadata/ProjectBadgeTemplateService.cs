using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.Services.Impl.Projects.Metadata;

internal class ProjectBadgeTemplateService(
    IProjectPropsService projectPropsService,
    ILogger<ProjectBadgeTemplateService> logger)
    : IProjectBadgeTemplateService
{
    public async Task<ProjectBadgeTemplateIdentification> CreateAsync(ProjectIdentification projectId, string name, BadgeLayout layout)
    {
        var entity = await projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.MustBeActive,
            new TemplateArgs(name, layout),
            ctx =>
            {
                var entity = new DataModel.ProjectBadgeTemplate
                {
                    ProjectId = projectId.Value,
                    Project = ctx.Project,
                    IsActive = true,
                };
                ctx.MarkCreatedNow(entity);
                Apply(ctx, entity, ctx.Request);
                ctx.Project.ProjectBadgeTemplates.Add(entity);
                return entity;
            });

        // ProjectBadgeTemplateId генерируется БД при SaveChanges — читаем уже после возврата из сервиса.
        var id = entity.GetId();
        logger.LogInformation("Создан шаблон бейджа {BadgeTemplateId} с именем {Name}", id, name);
        return id;
    }

    public async Task UpdateAsync(ProjectBadgeTemplateIdentification id, string name, BadgeLayout layout)
    {
        await projectPropsService.ChangeProjectProperties(
            id.ProjectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.MustBeActive,
            new TemplateArgs(name, layout),
            ctx =>
            {
                var entity = GetForChange(ctx, id);
                Apply(ctx, entity, ctx.Request);
            });
    }

    public async Task RemoveAsync(ProjectBadgeTemplateIdentification id) => await SetActive(id, isActive: false);

    public async Task RestoreAsync(ProjectBadgeTemplateIdentification id) => await SetActive(id, isActive: true);

    private async Task SetActive(ProjectBadgeTemplateIdentification id, bool isActive, [System.Runtime.CompilerServices.CallerMemberName] string operationName = "")
    {
        await projectPropsService.ChangeProjectProperties(
            id.ProjectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.MustBeActive,
            id,
            ctx => GetForChange(ctx, ctx.Request).IsActive = isActive,
            operationName);
    }

    /// <summary>Находит шаблон проекта и помечает его изменённым</summary>
    private static DataModel.ProjectBadgeTemplate GetForChange(ProjectMutationContext ctx, ProjectBadgeTemplateIdentification id)
    {
        var entity = ctx.Project.ProjectBadgeTemplates.SingleOrDefault(t => t.ProjectBadgeTemplateId == id.ProjectBadgeTemplateId)
            ?? throw new JoinRpgEntityNotFoundException(id.ProjectBadgeTemplateId, nameof(DataModel.ProjectBadgeTemplate));
        ctx.MarkChanged(entity);
        return entity;
    }

    private static void Apply(ProjectMutationContext ctx, DataModel.ProjectBadgeTemplate entity, TemplateArgs args)
    {
        var projectId = ctx.ProjectInfo.ProjectId;
        var name = args.Name.Trim();
        var layout = args.Layout;
        BadgeLayoutRules.EnsureValid(projectId, name, layout);

        var fieldIds = FieldIds(layout.Lines);
        var fields = ctx.Project.ProjectFields.Where(f => fieldIds.Contains(f.ProjectFieldId)).ToList();
        // Выключенное поле нельзя добавить в бейдж, но строка, уже стоявшая в шаблоне до выключения поля,
        // остаётся — иначе шаблон нельзя было бы пересохранить. Печать покажет её предупреждением.
        var alreadyUsed = FieldIds(entity.Lines);
        var available = fields.Where(f => f.IsActive || alreadyUsed.Contains(f.ProjectFieldId)).Select(f => f.ProjectFieldId).ToList();
        if (available.Count != fieldIds.Count)
        {
            throw new JoinRpgEntityNotFoundException(fieldIds.Except(available), nameof(DataModel.ProjectField));
        }

        // Поле, попавшее в бейдж, больше нельзя удалить окончательно — только выключить. Иначе бейдж
        // молча потерял бы строку; выключенное поле печать покажет предупреждением.
        foreach (var field in fields)
        {
            field.WasEverUsed = true;
        }

        entity.Name = name;
        entity.WidthMm = layout.WidthMm;
        entity.HeightMm = layout.HeightMm;
        entity.BackgroundUrl = layout.BackgroundUrl?.AbsoluteUri;
        entity.Lines = layout.Lines;
    }

    private static List<int> FieldIds(IEnumerable<BadgeLine> lines)
        => [.. lines.Where(l => l.ProjectFieldId is not null).Select(l => l.ProjectFieldId!.Value).Distinct()];

    /// <summary>Аргументы создания/изменения шаблона; <see cref="ToString"/> — ради лога.</summary>
    private record TemplateArgs(string Name, BadgeLayout Layout)
    {
        public override string ToString() => $"«{Name}» {Layout.WidthMm}×{Layout.HeightMm} мм, строк: {Layout.Lines.Count}";
    }
}
