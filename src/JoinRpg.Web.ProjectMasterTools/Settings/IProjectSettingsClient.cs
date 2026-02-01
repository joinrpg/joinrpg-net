namespace JoinRpg.Web.ProjectMasterTools.Settings;

public interface IProjectSettingsClient
{
    Task SavePublishSettings(ProjectPublishSettingsViewModel model);
    Task<ProjectPublishSettingsViewModel> GetPublishSettings(ProjectIdentification projectId);
    Task SaveContactSettings(ProjectContactsSettingsViewModel model);
    Task<ProjectContactsSettingsViewModel> GetContactSettings(ProjectIdentification projectId);
    Task SaveClaimSettings(ProjectClaimSettingsViewModel model);
    Task<ProjectClaimSettingsViewModel> GetClaimSettings(ProjectIdentification projectId);
}
