using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.FieldSetup;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.WebPortal.Managers;

/// <summary>
/// Operations on fields
/// </summary>
public class FieldSetupManager
{
    private ICurrentUserAccessor CurrentUser { get; }
    private IProjectMetadataRepository ProjectMetadataRepository { get; }
    private ICurrentProjectAccessor CurrentProject { get; }
    private IFieldSetupService Service { get; }

    /// <summary>
    /// ctor
    /// </summary>
    public FieldSetupManager(
        ICurrentUserAccessor currentUser,
        IProjectMetadataRepository projectMetadataRepository,
        ICurrentProjectAccessor currentProject,
        IFieldSetupService service
        )
    {
        CurrentUser = currentUser;
        ProjectMetadataRepository = projectMetadataRepository;
        CurrentProject = currentProject;
        Service = service;
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

    public async Task<FieldSettingsViewModel?> FillFailedSettingsModel(FieldSettingsViewModel model)
    {
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(CurrentProject.ProjectId);
        return FillSettingsModel(projectInfo, model);
    }

    /// <summary>
    /// Get field edit page
    /// </summary>
    public async Task<GameFieldEditViewModel?> EditPageAsync(int projectFieldId)
    {
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(CurrentProject.ProjectId);
        var field = projectInfo.UnsortedFields.SingleOrDefault(f => f.Id.ProjectFieldId == projectFieldId);
        if (field is null)
        {
            return null;
        }

        var model = new GameFieldEditViewModel(field, projectInfo, CurrentUser.UserIdentification);
        return FillFromProject(projectInfo, model);
    }

    /// <summary>
    /// Page with field settings
    /// </summary>
    public async Task<FieldSettingsViewModel?> SettingsPagesAsync()
    {
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(CurrentProject.ProjectId);

        var viewModel = new FieldSettingsViewModel
        {
            NameField = projectInfo.CharacterNameField?.Id.ProjectFieldId ?? -1,
            DescriptionField = projectInfo.CharacterDescriptionField?.Id.ProjectFieldId ?? -1,
        };
        return FillSettingsModel(projectInfo, viewModel);
    }

    private FieldSettingsViewModel FillSettingsModel(ProjectInfo projectInfo, FieldSettingsViewModel viewModel)
    {
        var fields = projectInfo.SortedFields;
        viewModel.PossibleDescriptionFields =
                ToSelectListItems(
                    fields.Where(f => f.Type == ProjectFieldType.Text && f.BoundTo == FieldBoundTo.Character),
                    "Нет поля с описанием персонажа"
                    ).SetSelected(projectInfo.CharacterDescriptionField?.Id.ProjectFieldId);
        viewModel.PossibleNameFields =
                ToSelectListItems(
                    fields.Where(f => f.Type == ProjectFieldType.String && f.BoundTo == FieldBoundTo.Character),
                    "Имя персонажа берется из имени игрока"
                    ).SetSelected(projectInfo.CharacterNameField?.Id.ProjectFieldId);

        // cast needed to call correct method
        var _ = FillFromProject(projectInfo, viewModel);
        return viewModel;
    }

    /// <summary>
    /// Set settings
    /// </summary>
    public async Task SettingsHandleAsync(FieldSettingsViewModel viewModel)
    {
        await Service.SetFieldSettingsAsync(new FieldSettingsRequest()
        {
            DescriptionField = viewModel.DescriptionField > 0 ? new(CurrentProject.ProjectId, viewModel.DescriptionField) : null,
            NameField = viewModel.NameField > 0 ? new(CurrentProject.ProjectId, viewModel.NameField) : null,
            ProjectId = CurrentProject.ProjectId,
        });
    }

    private List<JoinSelectListItem> ToSelectListItems(
        IEnumerable<ProjectFieldInfo> enumerable,
        string? notSelectedName = null
        )
    {
        var list = enumerable.Select(field => new JoinSelectListItem()
        {
            Value = field.Id.ProjectFieldId,
            Text = field.Name,

        }).ToList();

        if (notSelectedName != null)
        {

            list.Insert(0, new JoinSelectListItem()
            {
                Value = -1,
                Text = notSelectedName,
            });
        }

        return list;
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
            CanEditFields = projectInfo.HasMasterAccess(CurrentUser.UserIdentification, Permission.CanChangeFields)
                && projectInfo.IsActive,
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
