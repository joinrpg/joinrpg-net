using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectMasterTools.Settings;

namespace JoinRpg.WebPortal.Managers.Projects;

internal class ProjectSettingsViewService(IProjectMetadataRepository projectMetadataRepository, IProjectService projectService) : IProjectSettingsClient
{
    async Task<ProjectContactsSettingsViewModel> IProjectSettingsClient.GetContactSettings(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        return new ProjectContactsSettingsViewModel()
        {
            ProjectId = projectId,
            ProjectName = project.ProjectName,
            ProjectStatus = project.ProjectStatus,
            Fio = (MandatoryContactsView)project.ProfileRequirementSettings.RequireRealName,
            Phone = (MandatoryContactsView)project.ProfileRequirementSettings.RequirePhone,
            Telegram = (MandatoryContactsView)project.ProfileRequirementSettings.RequireTelegram,
            Vkontakte = (MandatoryContactsView)project.ProfileRequirementSettings.RequireVkontakte,
            Passport = project.ProfileRequirementSettings.RequirePassport == MandatoryStatus.Optional ? MandatorySenstiveContactsView.Optional : MandatorySenstiveContactsView.Request,
            RegistrationAddress = project.ProfileRequirementSettings.RequireRegistrationAddress == MandatoryStatus.Optional ? MandatorySenstiveContactsView.Optional : MandatorySenstiveContactsView.Request,
        };
    }

    async Task<ProjectPublishSettingsViewModel> IProjectSettingsClient.GetPublishSettings(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        return new ProjectPublishSettingsViewModel()
        {
            CloneSettings = (ProjectCloneSettingsView)project.CloneSettings,
            ProjectId = projectId,
            ProjectName = project.ProjectName,
            ProjectStatus = project.ProjectStatus,
            PublishEnabled = project.PublishPlot,
        };
    }

    async Task IProjectSettingsClient.SaveContactSettings(ProjectContactsSettingsViewModel model)
    {
        var settings = new ProjectProfileRequirementSettings((MandatoryStatus)model.Fio,
            (MandatoryStatus)model.Telegram,
            (MandatoryStatus)model.Vkontakte,
            (MandatoryStatus)model.Phone,
            model.Passport == MandatorySenstiveContactsView.Request ? MandatoryStatus.Recommended : MandatoryStatus.Optional,
            model.RegistrationAddress == MandatorySenstiveContactsView.Request ? MandatoryStatus.Recommended : MandatoryStatus.Optional
            );
        await projectService.SetContactSettings(model.ProjectId, settings);
    }

    async Task IProjectSettingsClient.SavePublishSettings(ProjectPublishSettingsViewModel model)
    {
        await projectService.SetPublishSettings(model.ProjectId, (ProjectCloneSettings)model.CloneSettings, model.PublishEnabled);
    }
}
