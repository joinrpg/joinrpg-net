using System.Text.Json;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl;

public class FieldSetupServiceImpl : DbServiceImplBase, IFieldSetupService
{

    public async Task<ProjectFieldIdentification> AddField(CreateFieldRequest request)
    {
        var project = await ProjectRepository.GetProjectAsync(request.ProjectId);

        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        if (project.GetTimeSlotFieldOrDefault() != null && request.FieldType == ProjectFieldType.ScheduleTimeSlotField)
        {
            throw new JoinFieldScheduleShouldBeUniqueException(project);
        }

        if (project.GetRoomFieldOrDefault() != null && request.FieldType == ProjectFieldType.ScheduleRoomField)
        {
            throw new JoinFieldScheduleShouldBeUniqueException(project);
        }

        var field = new ProjectField
        {
            ProjectId = request.ProjectId,
            Project = project,
            FieldType = request.FieldType,
            FieldBoundTo = request.FieldBoundTo,
        };

        await SetFieldPropertiesFromRequest(request, field);

        project.ProjectFields.Add(field);

        SetScheduleStatusBasedOnFields(project);

        _ = UnitOfWork.GetDbSet<ProjectField>().Add(field);
        await UnitOfWork.SaveChangesAsync();
        return new ProjectFieldIdentification(new ProjectIdentification(request.ProjectId), field.ProjectFieldId);
    }

    public async Task UpdateFieldParams(UpdateFieldRequest request)
    {
        var field = await ProjectRepository.GetProjectField(request.ProjectFieldId);

        _ = field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        // If we are changing field.CanPlayerEdit, we should update variants to match
        if (field.CanPlayerEdit != request.CanPlayerEdit)
        {
            foreach (var variant in field.DropdownValues)
            {
                variant.PlayerSelectable = request.CanPlayerEdit;
            }
        }

        await SetFieldPropertiesFromRequest(request, field);

        await UnitOfWork.SaveChangesAsync();
    }

    private async Task SetFieldPropertiesFromRequest(FieldRequestBase request, ProjectField field)
    {
        field.IsActive = true;

        field.FieldName = Required(request.Name);
        field.Description = new MarkdownString(request.FieldHint);
        field.MasterDescription = new MarkdownString(request.MasterFieldHint);
        field.CanPlayerEdit = request.CanPlayerEdit;
        field.CanPlayerView = request.CanPlayerView;
        field.ValidForNpc = request.ValidForNpc;
        field.IsPublic = request.IsPublic;
        field.MandatoryStatus = request.MandatoryStatus;
        field.AvailableForCharacterGroupIds =
            await ValidateCharacterGroupList(request.ProjectId, request.ShowForGroups);
        field.IncludeInPrint = request.IncludeInPrint;
        field.ShowOnUnApprovedClaims = request.ShowForUnapprovedClaims;
        field.Price = request.Price;
        field.ProgrammaticValue = request.ProgrammaticValue;

        CreateOrUpdateSpecialGroup(field);
    }

    public async Task DeleteField(int projectId, int projectFieldId)
    {
        ProjectField field = await ProjectRepository.GetProjectField(projectId, projectFieldId);

        _ = field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        var project = field.Project;
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

        await UnitOfWork.SaveChangesAsync();
    }

    public FieldSetupServiceImpl(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor) : base(unitOfWork, currentUserAccessor)
    {
    }

    public async Task<ProjectFieldVariantIdentification> CreateFieldValueVariant(CreateFieldValueVariantRequest request)
    {
        var field = await ProjectRepository.GetProjectField(request.ProjectFieldId);

        _ = field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        var variant = CreateFieldValueVariantImpl(request, field);

        await UnitOfWork.SaveChangesAsync();

        return new ProjectFieldVariantIdentification(request.ProjectFieldId, variant.ProjectFieldDropdownValueId);
    }


    public async Task UpdateFieldValueVariant(UpdateFieldValueVariantRequest request)
    {
        var field = await ProjectRepository.GetFieldValue(
            request.ProjectFieldId,
            request.ProjectFieldDropdownValueId);

        _ = field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        SetFieldVariantPropsFromRequest(request, field);

        await UnitOfWork.SaveChangesAsync();
    }

    private void SetFieldVariantPropsFromRequest(FieldValueVariantRequestBase request,
        ProjectFieldDropdownValue variant)
    {
        variant.Description = new MarkdownString(request.Description);
        variant.Label = request.Label;
        variant.IsActive = true;
        variant.MasterDescription = new MarkdownString(request.MasterDescription);

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

    public async Task<ProjectFieldDropdownValue> DeleteFieldValueVariant(int projectId, int projectFieldId, int valueId)
    {
        var value = await ProjectRepository.GetFieldValue(projectId, projectFieldId, valueId);
        _ = value.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);
        DeleteFieldVariantValueImpl(value);
        await UnitOfWork.SaveChangesAsync();
        return value;
    }

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

    public async Task MoveField(int projectId, int projectcharacterfieldid, short direction)
    {
        var field = await ProjectRepository.GetProjectField(projectId, projectcharacterfieldid);
        _ = field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        field.Project.Details.FieldsOrdering
            = field.Project.GetFieldsContainer().Move(field, direction).GetStoredOrder();
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveFieldVariant(int projectid, int projectFieldId, int projectFieldVariantId, short direction)
    {
        var field = await ProjectRepository.GetProjectField(projectid, projectFieldId);
        _ = field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        field.ValuesOrdering =
          field.GetFieldValuesContainer()
            .Move(field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == projectFieldVariantId), direction)
            .GetStoredOrder();

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task CreateFieldValueVariants(ProjectFieldIdentification projectFieldId, string valuesToAdd)
    {
        var field = await ProjectRepository.GetProjectField(projectFieldId);

        _ = field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        foreach (var label in valuesToAdd.Split('\n').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)))
        {
            CreateFieldValueVariantImpl(new CreateFieldValueVariantRequest(
                    projectFieldId,
                    label,
                    null,
                    null,
                    null,
                    0,
                    field.CanPlayerEdit,
                    timeSlotOptions: null),
                field);
        }

        await UnitOfWork.SaveChangesAsync();

    }

    public async Task MoveFieldAfter(int projectId, int projectFieldId, int? afterFieldId)
    {
        var field = await ProjectRepository.GetProjectField(projectId, projectFieldId);

        var afterField = afterFieldId == null
          ? null
          : await ProjectRepository.GetProjectField(projectId, (int)afterFieldId);

        _ = field.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        field.Project.Details.FieldsOrdering
            = field.Project.GetFieldsContainer().MoveAfter(field, afterField).GetStoredOrder();

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task SetFieldSettingsAsync(FieldSettingsRequest request)
    {
        var project = await ProjectRepository.GetProjectAsync(request.ProjectId);

        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields);

        project.Details.CharacterNameField = project.ProjectFields.SingleOrDefault(e => e.ProjectFieldId == request.NameField?.ProjectFieldId);
        project.Details.CharacterDescription = project.ProjectFields.SingleOrDefault(e => e.ProjectFieldId == request.DescriptionField?.ProjectFieldId);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task SortFieldVariants(int projectId, int projectFieldId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);

        var field = project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeFields).ProjectFields.Single(f => f.ProjectFieldId == projectFieldId);
        var container = field.GetFieldValuesContainer();
        container.SortBy(x => x.Label);
        field.ValuesOrdering = container.GetStoredOrder();

        await UnitOfWork.SaveChangesAsync();
    }

    private static void SetScheduleStatusBasedOnFields(Project project)
    {
        project.Details.ScheduleEnabled =
            project.GetTimeSlotFieldOrDefault() is not null && project.GetRoomFieldOrDefault() is not null;
    }
}
