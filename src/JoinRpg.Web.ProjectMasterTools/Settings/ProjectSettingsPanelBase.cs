using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.ProjectMasterTools.Settings;

/// <summary>
/// Панель настроек проекта: форма из <see cref="SettingsPanelBase{TModel}"/>, привязанная к проекту.
/// </summary>
public abstract class ProjectSettingsPanelBase<TModel> : SettingsPanelBase<TModel>
    where TModel : class
{
    [Inject]
    protected IProjectSettingsClient SettingsClient { get; set; } = null!;

    [Parameter]
    public ProjectIdentification ProjectId { get; set; } = null!;

    protected abstract Task<TModel> LoadModel(ProjectIdentification projectId);

    protected sealed override Task<TModel> LoadModel() => LoadModel(ProjectId);
}
