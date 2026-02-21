using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Services.Interfaces;

public interface IFieldSetupService
{
    Task UpdateFieldParams(UpdateFieldRequest request);

    Task DeleteField(int projectId, int projectFieldId);

    Task<ProjectFieldIdentification> AddField(CreateFieldRequest request);

    Task<ProjectFieldVariantIdentification> CreateFieldValueVariant(CreateFieldValueVariantRequest request);

    Task UpdateFieldValueVariant(UpdateFieldValueVariantRequest request);

    Task<ProjectFieldDropdownValue> DeleteFieldValueVariant(int projectId,
        int projectFieldId,
        int valueId);

    Task MoveField(int projectid, int projectcharacterfieldid, short direction);

    Task MoveFieldVariant(int projectid,
        int projectFieldId,
        int projectFieldVariantId,
        short direction);

    Task CreateFieldValueVariants(ProjectFieldIdentification projectFieldId,
        string valuesToAdd);

    Task MoveFieldAfter(int projectId, int projectFieldId, int? afterFieldId);

    Task SetFieldSettingsAsync(FieldSettingsRequest request);
    Task SortFieldVariants(int projectId, int projectFieldId);
}

public class FieldSettingsRequest
{
    public ProjectFieldIdentification? NameField { get; set; }
    public ProjectFieldIdentification? DescriptionField { get; set; }
    public required ProjectIdentification ProjectId { get; set; }
}

public abstract class FieldRequestBase(ProjectIdentification projectId,
    string name,
    string fieldHint,
    bool canPlayerEdit,
    bool canPlayerView,
    bool isPublic,
    MandatoryStatus mandatoryStatus,
    IReadOnlyCollection<CharacterGroupIdentification> showForGroups,
    bool validForNpc,
    bool includeInPrint,
    bool showForUnapprovedClaims,
    int price,
    string masterFieldHint,
    string? programmaticValue)
{
    public ProjectIdentification ProjectId { get; } = projectId;
    public string Name { get; } = name;
    public string FieldHint { get; } = fieldHint;

    public bool CanPlayerEdit { get; } = canPlayerEdit;
    public bool CanPlayerView { get; } = canPlayerView;
    public bool IsPublic { get; } = isPublic;
    public MandatoryStatus MandatoryStatus { get; } = mandatoryStatus;
    public IReadOnlyCollection<CharacterGroupIdentification> ShowForGroups { get; } = showForGroups;
    public bool ValidForNpc { get; } = validForNpc;
    public bool IncludeInPrint { get; } = includeInPrint;
    public bool ShowForUnapprovedClaims { get; } = showForUnapprovedClaims;
    public int Price { get; } = price;
    public string MasterFieldHint { get; } = masterFieldHint;
    public string? ProgrammaticValue { get; } = programmaticValue;
}

public sealed class CreateFieldRequest : FieldRequestBase
{
    public ProjectFieldType FieldType { get; }
    public FieldBoundTo FieldBoundTo { get; }

    /// <inheritdoc />
    public CreateFieldRequest(ProjectIdentification projectId,
        ProjectFieldType fieldType,
        string name,
        string fieldHint,
        bool canPlayerEdit,
        bool canPlayerView,
        bool isPublic,
        FieldBoundTo fieldBoundTo,
        MandatoryStatus mandatoryStatus,
        IReadOnlyCollection<CharacterGroupIdentification> showForGroups,
        bool validForNpc,
        bool includeInPrint,
        bool showForUnapprovedClaims,
        int price,
        string masterFieldHint,
        string? programmaticValue) : base(projectId,
        name,
        fieldHint,
        canPlayerEdit,
        canPlayerView,
        isPublic,
        mandatoryStatus,
        showForGroups,
        validForNpc,
        includeInPrint,
        showForUnapprovedClaims,
        price,
        masterFieldHint,
        programmaticValue)
    {
        FieldType = fieldType;
        FieldBoundTo = fieldBoundTo;
    }
}

public sealed class UpdateFieldRequest : FieldRequestBase
{
    /// <inheritdoc />
    public UpdateFieldRequest(ProjectFieldIdentification projectFieldId,
        string name,
        string fieldHint,
        bool canPlayerEdit,
        bool canPlayerView,
        bool isPublic,
        MandatoryStatus mandatoryStatus,
        IReadOnlyCollection<CharacterGroupIdentification> showForGroups,
        bool validForNpc,
        bool includeInPrint,
        bool showForUnapprovedClaims,
        int price,
        string masterFieldHint,
        string programmaticValue) : base(projectFieldId.ProjectId,
        name,
        fieldHint,
        canPlayerEdit,
        canPlayerView,
        isPublic,
        mandatoryStatus,
        showForGroups,
        validForNpc,
        includeInPrint,
        showForUnapprovedClaims,
        price,
        masterFieldHint,
        programmaticValue) => ProjectFieldId = projectFieldId;

    public ProjectFieldIdentification ProjectFieldId { get; }
}

public abstract class FieldValueVariantRequestBase
{
    public ProjectFieldIdentification ProjectFieldId { get; }
    public string Label { get; }
    public string? Description { get; }
    public string? MasterDescription { get; }
    public string? ProgrammaticValue { get; }
    public int Price { get; }
    public bool PlayerSelectable { get; }
    public TimeSlotOptions? TimeSlotOptions { get; }

    protected FieldValueVariantRequestBase(ProjectFieldIdentification projectFieldId,
        string label,
        string? description,
        string? masterDescription,
        string? programmaticValue,
        int price,
        bool playerSelectable,
        TimeSlotOptions? timeSlotOptions)
    {
        Label = label;
        Description = description;
        ProjectFieldId = projectFieldId;
        MasterDescription = masterDescription;
        ProgrammaticValue = programmaticValue;
        Price = price;
        PlayerSelectable = playerSelectable;
        TimeSlotOptions = timeSlotOptions;
    }
}

public class UpdateFieldValueVariantRequest : FieldValueVariantRequestBase
{
    public UpdateFieldValueVariantRequest(ProjectFieldIdentification projectFieldId,
        int projectFieldDropdownValueId,
        string label,
        string description,
        string masterDescription,
        string programmaticValue,
        int price,
        bool playerSelectable,
        TimeSlotOptions? timeSlotOptions)
        : base(projectFieldId,
            label,
            description,
            masterDescription,
            programmaticValue,
            price,
            playerSelectable,
            timeSlotOptions) => ProjectFieldDropdownValueId = projectFieldDropdownValueId;

    public int ProjectFieldDropdownValueId { get; }
}

public class CreateFieldValueVariantRequest : FieldValueVariantRequestBase
{
    public CreateFieldValueVariantRequest(ProjectFieldIdentification projectFieldId,
        string label,
        string? description,
        string? masterDescription,
        string? programmaticValue,
        int price,
        bool playerSelectable,
        TimeSlotOptions? timeSlotOptions)
        : base(projectFieldId,
            label,
            description,
            masterDescription,
            programmaticValue,
            price,
            playerSelectable,
            timeSlotOptions)
    {
    }
}
