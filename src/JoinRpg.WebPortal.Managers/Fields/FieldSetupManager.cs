using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.FieldSetup;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.WebPortal.Managers
{
    /// <summary>
    /// Operations on fields
    /// </summary>
    public class FieldSetupManager
    {
        private ICurrentUserAccessor CurrentUser { get; }
        private IProjectRepository ProjectRepository { get; }
        private ICurrentProjectAccessor CurrentProject { get; }
        private IFieldSetupService Service { get; }

        /// <summary>
        /// ctor
        /// </summary>
        public FieldSetupManager(
            ICurrentUserAccessor currentUser,
            IProjectRepository project,
            ICurrentProjectAccessor currentProject,
            IFieldSetupService service
            )
        {
            CurrentUser = currentUser;
            ProjectRepository = project;
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
            var project = await ProjectRepository.GetProjectWithFieldsAsync(CurrentProject.ProjectId);
            if (project == null)
            {
                return null;
            }
            return FillFromProject(project, new GameFieldCreateViewModel());
        }

        public async Task<T?> FillFailedModel<T>(T model) where T : class, IFieldNavigationAware
        {
            var project = await ProjectRepository.GetProjectWithFieldsAsync(CurrentProject.ProjectId);
            if (project == null)
            {
                return null;
            }
            return FillFromProject(project, model);
        }

        public async Task<FieldSettingsViewModel?> FillFailedSettingsModel(FieldSettingsViewModel model)
        {
            var project = await ProjectRepository.GetProjectWithFieldsAsync(CurrentProject.ProjectId);
            if (project == null)
            {
                return null;
            }
            return FillSettingsModel(project, model);
        }

        /// <summary>
        /// Get field edit page
        /// </summary>
        public async Task<GameFieldEditViewModel?> EditPageAsync(int projectFieldId)
        {
            var field = await ProjectRepository.GetProjectField(CurrentProject.ProjectId, projectFieldId);
            if (field == null)
            {
                return null;
            }

            var model = new GameFieldEditViewModel(field, CurrentUser.UserId);
            return FillFromProject(field.Project, model);
        }

        /// <summary>
        /// Page with field settings
        /// </summary>
        public async Task<FieldSettingsViewModel?> SettingsPagesAsync()
        {
            var project = await ProjectRepository.GetProjectWithFieldsAsync(CurrentProject.ProjectId);
            if (project == null)
            {
                return null;
            }

            var viewModel = new FieldSettingsViewModel
            {
                NameField = project.Details.CharacterNameField?.ProjectFieldId ?? -1,
                DescriptionField = project.Details.CharacterDescription?.ProjectFieldId ?? -1,
                LegacyModelEnabled = project.Details.CharacterNameLegacyMode,
            };
            return FillSettingsModel(project, viewModel);
        }

        private FieldSettingsViewModel FillSettingsModel(Project project, FieldSettingsViewModel viewModel)
        {
            var fields = project.GetOrderedFields();
            viewModel.PossibleDescriptionFields =
                    ToSelectListItems(
                        fields.Where(f => f.FieldType == ProjectFieldType.Text && f.FieldBoundTo == FieldBoundTo.Character),
                        "Нет поля с описанием персонажа"
                        ).SetSelected(project.Details.CharacterDescription?.ProjectFieldId);
            viewModel.PossibleNameFields =
                    ToSelectListItems(
                        fields.Where(f => f.FieldType == ProjectFieldType.String && f.FieldBoundTo == FieldBoundTo.Character),
                        "Имя персонажа берется из имени игрока"
                        ).SetSelected(project.Details.CharacterNameField?.ProjectFieldId);

            // cast needed to call correct method
            var _ = FillFromProject(project, viewModel);
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
                LegacyModelEnabled = viewModel.LegacyModelEnabled,
                ProjectId = CurrentProject.ProjectId,
            });
        }

        private List<JoinSelectListItem> ToSelectListItems(
            IEnumerable<ProjectField> enumerable,
            string? notSelectedName = null
            )
        {
            var list = enumerable.Select(field => new JoinSelectListItem()
            {
                Value = field.ProjectFieldId,
                Text = field.FieldName,

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
            Func<ProjectField, bool> predicate)
        {
            var project = await ProjectRepository.GetProjectWithFieldsAsync(CurrentProject.ProjectId);
            if (project == null)
            {
                return null;
            }
            var fields = project.GetOrderedFields().Where(predicate).ToViewModels(CurrentUser.UserId);
            FieldNavigationModel navigation = GetNavigation(page, project);
            return new GameFieldListViewModel(navigation, fields);
        }

        private FieldNavigationModel GetNavigation(FieldNavigationPage page, Project project)
        {
            return new FieldNavigationModel
            {
                Page = page,
                ProjectId = project.ProjectId,
                CanEditFields = project.HasMasterAccess(CurrentUser.UserId, pa => pa.CanChangeFields)
                    && project.Active,
            };
        }

        private T FillFromProject<T>(
            Project project,
            T viewModel) where T : IFieldNavigationAware
        {
            viewModel.SetNavigation(GetNavigation(FieldNavigationPage.Unknown, project));
            viewModel.ProjectId = project.ProjectId;
            return viewModel;
        }
    }
}
