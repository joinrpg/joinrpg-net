using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    public class FieldSetupServiceImpl: DbServiceImplBase, IFieldSetupService
    {

        public async Task AddField(int projectId,
            ProjectFieldType fieldType,
            string name,
            string fieldHint,
            bool canPlayerEdit,
            bool canPlayerView,
            bool isPublic,
            FieldBoundTo fieldBoundTo,
            MandatoryStatus mandatoryStatus,
            List<int> showForGroups,
            bool validForNpc,
            bool includeInPrint,
            bool showForUnapprovedClaims,
            int price,
            string masterFieldHint)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);

            project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            var field = new ProjectField
            {
                FieldType = fieldType,
                FieldBoundTo = fieldBoundTo,

                FieldName = Required(name),
                Description = new MarkdownString(fieldHint),
                MasterDescription = new MarkdownString(masterFieldHint),
                CanPlayerEdit = canPlayerEdit,
                CanPlayerView = canPlayerView,
                ValidForNpc = validForNpc,
                IsPublic = isPublic,
                ProjectId = projectId,
                Project = project, //We require it for CreateOrUpdateSpecailGroup
                IsActive = true,
                MandatoryStatus = mandatoryStatus,
                AvailableForCharacterGroupIds =
                    await ValidateCharacterGroupList(projectId, showForGroups),
                IncludeInPrint = includeInPrint,
                ShowOnUnApprovedClaims = showForUnapprovedClaims,
                Price = price,
            };

            CreateOrUpdateSpecialGroup(field);

            UnitOfWork.GetDbSet<ProjectField>().Add(field);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task UpdateFieldParams(int projectId,
            int fieldId,
            string name,
            string fieldHint,
            bool canPlayerEdit,
            bool canPlayerView,
            bool isPublic,
            MandatoryStatus mandatoryStatus,
            List<int> showForGroups,
            bool validForNpc,
            bool includeInPrint,
            bool showForUnapprovedClaims,
            int price,
            string masterFieldHint)
        {
            var field = await ProjectRepository.GetProjectField(projectId, fieldId);

            field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            // If we are changing field.CanPlayerEdit, we should update variants to match
            if (field.CanPlayerEdit != canPlayerEdit)
            {
                foreach (var variant in field.DropdownValues)
                {
                    variant.PlayerSelectable = canPlayerEdit;
                }
            }

            field.FieldName = Required(name);
            field.Description = new MarkdownString(fieldHint);
            field.MasterDescription = new MarkdownString(masterFieldHint);
            field.CanPlayerEdit = canPlayerEdit;
            field.CanPlayerView = canPlayerView;
            field.IsPublic = isPublic;
            field.IsActive = true;
            field.MandatoryStatus = mandatoryStatus;
            field.ValidForNpc = validForNpc;
            field.AvailableForCharacterGroupIds =
                await ValidateCharacterGroupList(projectId, showForGroups);
            field.IncludeInPrint = includeInPrint;
            field.ShowOnUnApprovedClaims = showForUnapprovedClaims;
            field.Price = price;

            CreateOrUpdateSpecialGroup(field);

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task<ProjectField> DeleteField(int projectId, int projectFieldId)
        {
            ProjectField field = await ProjectRepository.GetProjectField(projectId, projectFieldId);
            await DeleteField(field);
            return field;
        }

        /// <summary>
        /// Deletes field by its object. We assume here that field represents really existed field in really existed project
        /// </summary>
        /// <param name="field">Field to delete</param>
        public async Task DeleteField(ProjectField field)
        {
            field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            foreach (var fieldValueVariant in field.DropdownValues.ToArray()) //Required, cause we modify fields inside.
            {
                DeleteFieldVariantValueImpl(fieldValueVariant);
            }

            var characterGroup = field.CharacterGroup; // SmartDelete will nullify all depend properties
            if (SmartDelete(field))
            {
                SmartDelete(characterGroup);
            }
            else if (characterGroup != null)
            {
                characterGroup.IsActive = false;
            }
            await UnitOfWork.SaveChangesAsync();
        }

        public FieldSetupServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public async Task CreateFieldValueVariant(CreateFieldValueVariantRequest request)
        {
            var field = await ProjectRepository.GetProjectField(request.ProjectId, request.ProjectFieldId);

            field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            CreateFieldValueVariantImpl(request, field);

            await UnitOfWork.SaveChangesAsync();
        }

        private void CreateFieldValueVariantImpl(CreateFieldValueVariantRequest request, ProjectField field)
        {
            var fieldValue = new ProjectFieldDropdownValue()
            {
                Description = new MarkdownString(request.Description),
                Label = request.Label,
                IsActive = true,
                WasEverUsed = false,
                ProjectId = field.ProjectId,
                ProjectFieldId = field.ProjectFieldId,
                Project = field.Project,
                ProjectField = field,
                MasterDescription = new MarkdownString(request.MasterDescription),
                ProgrammaticValue = request.ProgrammaticValue,
                Price = request.Price,
                PlayerSelectable = request.PlayerSelectable && field.CanPlayerEdit,
            };

            CreateOrUpdateSpecialGroup(fieldValue);

            field.DropdownValues.Add(fieldValue);
        }

        private void CreateOrUpdateSpecialGroup(ProjectFieldDropdownValue fieldValue)
        {
            var field = fieldValue.ProjectField;
            if (!field.HasSpecialGroup())
            {
                return;
            }
            CreateOrUpdateSpecialGroup(field);

            if (fieldValue.CharacterGroup == null)
            {
                fieldValue.CharacterGroup = new CharacterGroup()
                {
                    AvaiableDirectSlots = 0,
                    HaveDirectSlots = false,
                    ParentCharacterGroupIds = new[] { field.CharacterGroup.CharacterGroupId },
                    ProjectId = fieldValue.ProjectId,
                    IsRoot = false,
                    IsSpecial = true,
                    ResponsibleMasterUserId = null,
                };
                MarkCreatedNow(fieldValue.CharacterGroup);
            }
            UpdateSpecialGroupProperties(fieldValue);
        }

        private void UpdateSpecialGroupProperties(ProjectFieldDropdownValue fieldValue)
        {
            var field = fieldValue.ProjectField;
            var characterGroup = fieldValue.CharacterGroup;
            var specialGroupName = fieldValue.GetSpecialGroupName();

            Debug.Assert(characterGroup != null, "characterGroup != null");

            if (characterGroup.IsPublic != field.IsPublic ||
                characterGroup.IsActive != fieldValue.IsActive ||
                characterGroup.Description != fieldValue.Description ||
                characterGroup.CharacterGroupName != specialGroupName)
            {
                characterGroup.IsPublic = field.IsPublic;
                characterGroup.IsActive = fieldValue.IsActive;
                characterGroup.Description = fieldValue.Description;
                characterGroup.CharacterGroupName = specialGroupName;
                MarkChanged(characterGroup);
            }
        }

        private void CreateOrUpdateSpecialGroup(ProjectField field)
        {
            if (!field.HasSpecialGroup())
            {
                return;
            }

            if (field.CharacterGroup == null)
            {
                field.CharacterGroup = new CharacterGroup()
                {
                    AvaiableDirectSlots = 0,
                    HaveDirectSlots = false,
                    ParentCharacterGroupIds = new[] { field.Project.RootGroup.CharacterGroupId },
                    ProjectId = field.ProjectId,
                    IsRoot = false,
                    IsSpecial = true,
                    ResponsibleMasterUserId = null,
                };
                MarkCreatedNow(field.CharacterGroup);
            }

            foreach (var fieldValue in field.DropdownValues)
            {
                if (fieldValue.CharacterGroup == null)
                    continue; //We can't convert to LINQ because of RSRP-457084
                UpdateSpecialGroupProperties(fieldValue);
            }

            UpdateSpecialGroupProperties(field);
        }

        private void UpdateSpecialGroupProperties(ProjectField field)
        {
            var characterGroup = field.CharacterGroup;
            var specialGroupName = field.GetSpecialGroupName();

            if (characterGroup.IsPublic != field.IsPublic || characterGroup.IsActive != field.IsActive ||
                characterGroup.Description != field.Description ||
                characterGroup.CharacterGroupName != specialGroupName)
            {
                characterGroup.IsPublic = field.IsPublic;
                characterGroup.IsActive = field.IsActive;
                characterGroup.Description = field.Description;
                characterGroup.CharacterGroupName = specialGroupName;
                MarkChanged(characterGroup);
            }
        }

        public async Task UpdateFieldValueVariant(UpdateFieldValueVariantRequest request)
        {
            var field = await ProjectRepository.GetFieldValue(request.ProjectId, request.ProjectFieldId, request.ProjectFieldDropdownValueId);

            field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            field.Description = new MarkdownString(request.Description);
            field.Label = request.Label;
            field.IsActive = true;
            field.MasterDescription = new MarkdownString(request.MasterDescription);
            field.ProgrammaticValue = request.ProgrammaticValue;
            field.Price = request.Price;
            field.PlayerSelectable = request.PlayerSelectable;

            CreateOrUpdateSpecialGroup(field);

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task<ProjectFieldDropdownValue> DeleteFieldValueVariant(int projectId, int projectFieldId, int valueId)
        {
            var value = await ProjectRepository.GetFieldValue(projectId, projectFieldId, valueId);
            value.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);
            DeleteFieldVariantValueImpl(value);
            await UnitOfWork.SaveChangesAsync();
            return value;
        }

        private void DeleteFieldVariantValueImpl(ProjectFieldDropdownValue value)
        {
            var characterGroup = value.CharacterGroup; // SmartDelete will nullify all depend properties
            if (SmartDelete(value))
            {
                SmartDelete(characterGroup);
            }
            else
            {
                if (characterGroup != null)
                {
                    characterGroup.IsActive = false;
                }
            }
        }

        public async Task MoveField(int projectId, int projectcharacterfieldid, short direction)
        {
            var field = await ProjectRepository.GetProjectField(projectId, projectcharacterfieldid);
            field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            field.Project.ProjectFieldsOrdering = field.Project.GetFieldsContainer().Move(field, direction).GetStoredOrder();
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task MoveFieldVariant(int projectid, int projectFieldId, int projectFieldVariantId, short direction)
        {
            var field = await ProjectRepository.GetProjectField(projectid, projectFieldId);
            field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            field.ValuesOrdering =
              field.GetFieldValuesContainer()
                .Move(field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == projectFieldVariantId), direction)
                .GetStoredOrder();

            await UnitOfWork.SaveChangesAsync();
        }

        public async Task CreateFieldValueVariants(int projectId, int projectFieldId, string valuesToAdd)
        {
            var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);

            field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            foreach (var label in valuesToAdd.Split('\n').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)))
            {
                CreateFieldValueVariantImpl(new CreateFieldValueVariantRequest(field.ProjectId, label, null, field.ProjectFieldId, null, null, 0, true), field);
            }

            await UnitOfWork.SaveChangesAsync();

        }

        public async Task MoveFieldAfter(int projectId, int projectFieldId, int? afterFieldId)
        {
            var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);

            var afterField = afterFieldId == null
              ? null
              : await ProjectRepository.GetProjectField(projectId, (int)afterFieldId);

            field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

            field.Project.ProjectFieldsOrdering = field.Project.GetFieldsContainer().MoveAfter(field, afterField).GetStoredOrder();

            await UnitOfWork.SaveChangesAsync();
        }
    }
}
