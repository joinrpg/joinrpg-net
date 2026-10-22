using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.Badges;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.Badges;

internal class BadgeTemplateViewService(
    IProjectBadgeTemplateRepository repository,
    IProjectBadgeTemplateService service,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository)
    : IBadgeTemplateClient
{
    public async Task<BadgeTemplateListViewModel> GetList(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var templates = await repository.GetForProjectAsync(projectId);

        return new BadgeTemplateListViewModel(
            [.. templates
                .OrderByDescending(t => t.IsActive)
                .ThenBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(t => new BadgeTemplateListItemViewModel(t.Id, t.Name, t.Layout.WidthMm, t.Layout.HeightMm, t.IsActive))],
            HasEditAccess: projectInfo.HasMasterAccess(currentUserAccessor.UserIdentification, Permission.CanChangeProjectProperties));
    }

    public async Task<EditBadgeTemplateViewModel> GetById(ProjectBadgeTemplateIdentification id)
    {
        // Удалённый шаблон на правку не открываем — сначала его восстанавливают из списка.
        var template = await repository.GetByIdAsync(id);
        if (template is not { IsActive: true })
        {
            throw new JoinRpgEntityNotFoundException(id.ProjectBadgeTemplateId, nameof(ProjectBadgeTemplate));
        }
        return EditBadgeTemplateViewModel.FromDomain(template);
    }

    public Task<ProjectBadgeTemplateIdentification> Create(ProjectIdentification projectId, EditBadgeTemplateViewModel model)
        => service.CreateAsync(projectId, model.Name, model.ToLayout());

    public Task Update(ProjectBadgeTemplateIdentification id, EditBadgeTemplateViewModel model)
        => service.UpdateAsync(id, model.Name, model.ToLayout());

    public Task Remove(ProjectBadgeTemplateIdentification id) => service.RemoveAsync(id);

    public Task Restore(ProjectBadgeTemplateIdentification id) => service.RestoreAsync(id);
}
