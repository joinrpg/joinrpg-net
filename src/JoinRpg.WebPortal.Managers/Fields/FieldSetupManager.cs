using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectMasterTools.Fields;
using JoinRpg.WebPortal.Managers.Fields;
using JoinRpg.WebPortal.Managers.Interfaces;
using JoinRpg.WebPortal.Models.FieldSetup;

namespace JoinRpg.WebPortal.Managers;

/// <summary>
/// Operations on fields
/// </summary>
public class FieldSetupManager
{
    private ICurrentUserAccessor CurrentUser { get; }
    private IProjectMetadataRepository ProjectMetadataRepository { get; }
    private ICurrentProjectAccessor CurrentProject { get; }

    /// <summary>
    /// ctor
    /// </summary>
    public FieldSetupManager(
        ICurrentUserAccessor currentUser,
        IProjectMetadataRepository projectMetadataRepository,
        ICurrentProjectAccessor currentProject
        )
    {
        CurrentUser = currentUser;
        ProjectMetadataRepository = projectMetadataRepository;
        CurrentProject = currentProject;
    }

    /// <summary>
    /// Get all active fields
    /// </summary>
    public async Task<GameFieldListViewModel?> GetActiveAsync() => await GetFieldsImpl(FieldNavigationPage.ActiveFieldsList, field => field.IsActive);

    /// <summary>
    /// Get all inactive fields
    /// </summary>
    public async Task<GameFieldListViewModel?> GetInActiveAsync() => await GetFieldsImpl(FieldNavigationPage.DeletedFieldsList, field => !field.IsActive);

    /// <summary>
    /// Get field creation page
    /// </summary>
    public async Task<GameFieldCreateViewModel?> CreatePageAsync()
    {
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(CurrentProject.ProjectId);
        return FillFromProject(projectInfo, new GameFieldCreateViewModel());
    }

    public async Task<T?> FillFailedModel<T>(T model) where T : class, IFieldNavigationAware
    {
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(CurrentProject.ProjectId);
        return FillFromProject(projectInfo, model);
    }

    /// <summary>
    /// Get field edit page
    /// </summary>
    public async Task<GameFieldEditViewModel?> EditPageAsync(ProjectFieldIdentification projectFieldId)
    {
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(CurrentProject.ProjectId);
        var field = projectInfo.GetFieldById(projectFieldId);
        if (field is null)
        {
            return null;
        }

        var model = new GameFieldEditViewModel(field);
        return FillFromProject(projectInfo, model);
    }

    private async Task<GameFieldListViewModel?> GetFieldsImpl(
        FieldNavigationPage page,
        Func<ProjectFieldInfo, bool> predicate)
    {
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(CurrentProject.ProjectId);
        var fields = projectInfo.SortedFields.Where(predicate).ToViewModels(projectInfo, CurrentUser.UserIdentification);
        FieldNavigationModel navigation = GetNavigation(page, projectInfo);
        return new GameFieldListViewModel(navigation, fields);
    }

    private FieldNavigationModel GetNavigation(FieldNavigationPage page, ProjectInfo projectInfo)
    {
        return new FieldNavigationModel
        {
            Page = page,
            ProjectId = projectInfo.ProjectId,
            DefaultTemplateCharacterId = projectInfo.ClaimSettings.DefaultTemplate,
            UnboundNameFieldCandidates = [.. projectInfo.GetUnboundCharacterNameFieldCandidates().Select(f => f.Name)],
        };
    }

    private T FillFromProject<T>(
        ProjectInfo projectInfo,
        T viewModel) where T : IFieldNavigationAware
    {
        viewModel.SetNavigation(GetNavigation(FieldNavigationPage.Unknown, projectInfo));
        viewModel.ProjectId = projectInfo.ProjectId;
        return viewModel;
    }
}
