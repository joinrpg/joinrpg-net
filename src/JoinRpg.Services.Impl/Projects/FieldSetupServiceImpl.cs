using System.Text.Json;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;

namespace JoinRpg.Services.Impl.Projects;

internal class FieldSetupServiceImpl(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectPropsService projectPropsService)
    : DbServiceImplBase(unitOfWork, currentUserAccessor), IFieldSetupService
{
    public async Task<ProjectFieldIdentification> AddField(CreateFieldRequest request)
    {
        var field = await projectPropsService.ChangeProjectProperties(
            request.ProjectId,
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            request,
            (project, projectInfo, req) =>
            {
                if (project.GetTimeSlotFieldOrDefault() != null && req.FieldType == ProjectFieldType.ScheduleTimeSlotField)
                {
                    throw new JoinFieldScheduleShouldBeUniqueException(project);
                }

                if (project.GetRoomFieldOrDefault() != null && req.FieldType == ProjectFieldType.ScheduleRoomField)
                {
                    throw new JoinFieldScheduleShouldBeUniqueException(project);
                }

                var field = new ProjectField
                {
                    ProjectId = req.ProjectId,
                    Project = project,
                    FieldType = req.FieldType,
                    FieldBoundTo = req.FieldBoundTo,
                };

                SetFieldPropertiesFromRequest(req, field, projectInfo);

                project.ProjectFields.Add(field);

                SetScheduleStatusBasedOnFields(project);

                return field;
            });

        // ProjectFieldId генерируется БД при SaveChanges — читаем уже после возврата из сервиса.
        return field.GetId();
    }

    public async Task UpdateFieldParams(UpdateFieldRequest request)
    {
        await projectPropsService.ChangeProjectProperties(
            request.ProjectId,
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            request,
            (project, projectInfo, req) =>
            {
                var field = GetField(project, req.ProjectFieldId.ProjectFieldId);

                // If we are changing field.CanPlayerEdit, we should update variants to match
                if (field.CanPlayerEdit != req.CanPlayerEdit)
                {
                    foreach (var variant in field.DropdownValues)
                    {
                        variant.PlayerSelectable = req.CanPlayerEdit;
                    }
                }

                SetFieldPropertiesFromRequest(req, field, projectInfo);
            });
    }

    public async Task DeleteField(int projectId, int projectFieldId)
    {
        await projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(projectId),
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            projectFieldId,
            (project, projectInfo, fieldId) =>
            {
                var field = GetField(project, fieldId);

                if (field.IsName())
                {
                    throw new JoinRpgNameFieldDeleteException(field);
                }

                if (field.IsDescription())
                {
                    project.Details.CharacterDescription = null;
                }

                foreach (var fieldValueVariant in field.DropdownValues.ToArray()) //Required, cause we modify fields inside.
                {
                    DeleteFieldVariantValueImpl(fieldValueVariant);
                }

                var characterGroup = field.CharacterGroup; // SmartDelete will nullify all depend properties
                if (SmartDelete(field))
                {
                    _ = SmartDelete(characterGroup);
                }
                else if (characterGroup != null)
                {
                    characterGroup.IsActive = false;
                }

                SetScheduleStatusBasedOnFields(project);
            });
    }

    public async Task<ProjectFieldVariantIdentification> CreateFieldValueVariant(CreateFieldValueVariantRequest request)
    {
        var variant = await projectPropsService.ChangeProjectProperties(
            request.ProjectFieldId.ProjectId,
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            request,
            (project, _, req) =>
            {
                var field = GetField(project, req.ProjectFieldId.ProjectFieldId);
                return CreateFieldValueVariantImpl(req, field);
            });

        return new ProjectFieldVariantIdentification(request.ProjectFieldId, variant.ProjectFieldDropdownValueId);
    }

    public async Task UpdateFieldValueVariant(UpdateFieldValueVariantRequest request)
    {
        await projectPropsService.ChangeProjectProperties(
            request.ProjectFieldId.ProjectId,
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            request,
            (project, _, req) =>
            {
                var variant = GetFieldValue(project, req.ProjectFieldId.ProjectFieldId, req.ProjectFieldDropdownValueId);
                SetFieldVariantPropsFromRequest(req, variant);
            });
    }

    public async Task<ProjectFieldDropdownValue> DeleteFieldValueVariant(int projectId, int projectFieldId, int valueId)
    {
        return await projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(projectId),
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            (projectFieldId, valueId),
            (project, _, args) =>
            {
                var value = GetFieldValue(project, args.projectFieldId, args.valueId);
                DeleteFieldVariantValueImpl(value);
                return value;
            });
    }

    public async Task MoveField(int projectId, int projectcharacterfieldid, short direction)
    {
        await projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(projectId),
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            (projectcharacterfieldid, direction),
            (project, _, args) =>
            {
                var field = GetField(project, args.projectcharacterfieldid);
                project.Details.FieldsOrdering
                    = project.GetFieldsContainer().Move(field, args.direction).GetStoredOrder();
            });
    }

    public async Task MoveFieldVariant(int projectid, int projectFieldId, int projectFieldVariantId, short direction)
    {
        await projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(projectid),
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            (projectFieldId, projectFieldVariantId, direction),
            (project, _, args) =>
            {
                var field = GetField(project, args.projectFieldId);
                field.ValuesOrdering =
                    field.GetFieldValuesContainer()
                        .Move(field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == args.projectFieldVariantId), args.direction)
                        .GetStoredOrder();
            });
    }

    public async Task CreateFieldValueVariants(ProjectFieldIdentification projectFieldId, string valuesToAdd)
    {
        await projectPropsService.ChangeProjectProperties(
            projectFieldId.ProjectId,
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            (projectFieldId, valuesToAdd),
            (project, _, args) =>
            {
                var field = GetField(project, args.projectFieldId.ProjectFieldId);

                foreach (var label in args.valuesToAdd.Split('\n').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)))
                {
                    CreateFieldValueVariantImpl(new CreateFieldValueVariantRequest(
                            args.projectFieldId,
                            label,
                            null,
                            null,
                            null,
                            0,
                            field.CanPlayerEdit,
                            timeSlotOptions: null),
                        field);
                }
            });
    }

    public async Task MoveFieldAfter(int projectId, int projectFieldId, int? afterFieldId)
    {
        await projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(projectId),
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            (projectFieldId, afterFieldId),
            (project, _, args) =>
            {
                var field = GetField(project, args.projectFieldId);
                var afterField = args.afterFieldId == null
                    ? null
                    : GetField(project, (int)args.afterFieldId);

                project.Details.FieldsOrdering
                    = project.GetFieldsContainer().MoveAfter(field, afterField).GetStoredOrder();
            });
    }

    public async Task SetFieldSettingsAsync(FieldSettingsRequest request)
    {
        await projectPropsService.ChangeProjectProperties(
            request.ProjectId,
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            request,
            (project, _, req) =>
            {
                project.Details.CharacterNameField = project.ProjectFields.SingleOrDefault(e => e.ProjectFieldId == req.NameField?.ProjectFieldId);
                project.Details.CharacterDescription = project.ProjectFields.SingleOrDefault(e => e.ProjectFieldId == req.DescriptionField?.ProjectFieldId);
            });
    }

    public async Task SortFieldVariants(int projectId, int projectFieldId)
    {
        await projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(projectId),
            Permission.CanChangeFields,
            ProjectActiveRequirement.MustBeActive,
            projectFieldId,
            (project, _, fieldId) =>
            {
                var field = GetField(project, fieldId);
                var container = field.GetFieldValuesContainer();
                container.SortBy(x => x.Label);
                field.ValuesOrdering = container.GetStoredOrder();
            });
    }

    private static ProjectField GetField(Project project, int projectFieldId)
        => project.ProjectFields.Single(f => f.ProjectFieldId == projectFieldId);

    private static ProjectFieldDropdownValue GetFieldValue(Project project, int projectFieldId, int valueId)
        => GetField(project, projectFieldId).DropdownValues.Single(v => v.ProjectFieldDropdownValueId == valueId);

    private void DeleteFieldVariantValueImpl(ProjectFieldDropdownValue value)
    {
        var characterGroup = value.CharacterGroup; // SmartDelete will nullify all depend properties
        if (SmartDelete(value))
        {
            _ = SmartDelete(characterGroup);
        }
        else
        {
            if (characterGroup != null)
            {
                characterGroup.IsActive = false;
            }
        }
    }


    private static void SetScheduleStatusBasedOnFields(Project project)
    {
        project.Details.ScheduleEnabled =
            project.GetTimeSlotFieldOrDefault() is not null && project.GetRoomFieldOrDefault() is not null;
    }

    private void SetFieldPropertiesFromRequest(FieldRequestBase request, ProjectField field, ProjectInfo projectInfo)
    {
        field.IsActive = true;

        field.FieldName = Required(request.Name);
        field.Description = new MarkdownDbValue(request.FieldHint);
        field.MasterDescription = new MarkdownDbValue(request.MasterFieldHint);
        field.CanPlayerEdit = request.CanPlayerEdit;
        field.CanPlayerView = request.CanPlayerView;
        field.ValidForNpc = request.ValidForNpc;
        field.IsPublic = request.IsPublic;
        field.MandatoryStatus = request.MandatoryStatus;
        field.AvailableForCharacterGroupIds =
            ValidateCharacterGroupList(projectInfo, request.ShowForGroups);
        field.IncludeInPrint = request.IncludeInPrint;
        field.ShowOnUnApprovedClaims = request.ShowForUnapprovedClaims;
        field.Price = request.Price;
        field.ProgrammaticValue = request.ProgrammaticValue;

        CreateOrUpdateSpecialGroup(field);
    }

    private void SetFieldVariantPropsFromRequest(FieldValueVariantRequestBase request,
    ProjectFieldDropdownValue variant)
    {
        variant.Description = new MarkdownDbValue(request.Description);
        variant.Label = request.Label;
        variant.IsActive = true;
        variant.MasterDescription = new MarkdownDbValue(request.MasterDescription);

        variant.Price = request.Price;
        variant.PlayerSelectable = request.PlayerSelectable;
        if (variant.ProjectField.IsTimeSlot())
        {
            SetTimeSlotOptions(variant, request.TimeSlotOptions);
        }

        else
        {
            variant.ProgrammaticValue = request.ProgrammaticValue;
        }


        CreateOrUpdateSpecialGroup(variant);
    }

    private static void SetTimeSlotOptions(ProjectFieldDropdownValue self, TimeSlotOptions? timeSlotOptions)
    {
        if (!self.ProjectField.IsTimeSlot())
        {
            throw new Exception("That's not time slot'");
        }

        self.ProgrammaticValue = JsonSerializer.Serialize(timeSlotOptions);
    }

    private ProjectFieldDropdownValue CreateFieldValueVariantImpl(CreateFieldValueVariantRequest request, ProjectField field)
    {
        var fieldVariant = new ProjectFieldDropdownValue()
        {
            WasEverUsed = false,
            ProjectId = field.ProjectId,
            ProjectFieldId = field.ProjectFieldId,
            Project = field.Project,
            ProjectField = field,
        };

        SetFieldVariantPropsFromRequest(request, fieldVariant);

        field.DropdownValues.Add(fieldVariant);
        return fieldVariant;
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
                ParentCharacterGroupIds = [field.CharacterGroup!.CharacterGroupId], // CreateOrUpdateSpecialGroup (field) ensures this
                ProjectId = fieldValue.ProjectId,
                IsRoot = false,
                IsSpecial = true,
                ResponsibleMasterUserId = null,
            };
            MarkCreatedNow(fieldValue.CharacterGroup);
        }
        UpdateSpecialGroupProperties(fieldValue, fieldValue.CharacterGroup);
    }

    // Character group i bound into fieldValue here, but it passed implicitly for removing null warning
    private void UpdateSpecialGroupProperties(ProjectFieldDropdownValue fieldValue, CharacterGroup characterGroup)
    {
        var field = fieldValue.ProjectField;
        var specialGroupName = fieldValue.GetSpecialGroupName();

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
                CharacterGroupId = -field.ProjectFieldId, // We will need this if create both field and variant special group at once.
                ParentCharacterGroupIds = [field.Project.RootGroup.CharacterGroupId],
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
            {
                continue; //We can't convert to LINQ because of RSRP-457084
            }

            UpdateSpecialGroupProperties(fieldValue, fieldValue.CharacterGroup);
        }

        UpdateSpecialGroupProperties(field, field.CharacterGroup);
    }

    private void UpdateSpecialGroupProperties(ProjectField field, CharacterGroup characterGroup)
    {
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
}
