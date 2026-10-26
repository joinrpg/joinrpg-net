namespace JoinRpg.Web.ProjectMasterTools.Settings;

public interface IProjectSettingsClient
{
    Task SaveMainSettings(ProjectMainSettingsViewModel model);
    Task<ProjectMainSettingsViewModel> GetMainSettings(ProjectIdentification projectId);
    Task SavePublishSettings(ProjectPublishSettingsViewModel model);
    Task<ProjectPublishSettingsViewModel> GetPublishSettings(ProjectIdentification projectId);
    Task SaveContactSettings(ProjectContactsSettingsViewModel model);
    Task<ProjectContactsSettingsViewModel> GetContactSettings(ProjectIdentification projectId);
    Task SaveClaimSettings(ProjectClaimSettingsViewModel model);
    Task<ProjectClaimSettingsViewModel> GetClaimSettings(ProjectIdentification projectId);
    Task SaveTimeZoneSettings(ProjectTimeZoneSettingsViewModel model);
    Task<ProjectTimeZoneSettingsViewModel> GetTimeZoneSettings(ProjectIdentification projectId);
}
