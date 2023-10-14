using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;
public record class ProjectFieldInfo(
    ProjectFieldIdentification Id,
    string Name,
    ProjectFieldType Type,
    FieldBoundTo BoundTo,
    IReadOnlyCollection<ProjectFieldVariant> Variants,
    string Ordering,
    int Price,
    bool CanPlayerEdit,
    bool ShowOnUnApprovedClaims,
    bool IsPublic,
    bool CanPlayerView,
    MandatoryStatus MandatoryStatus,
    bool ValidForNpc,
    bool IsActive,
    IReadOnlyCollection<int> GroupsAvailableForIds,
    MarkdownString Description,
    MarkdownString MasterDescription,
    bool IncludeInPrint,
    ProjectFieldSettings FieldSettings,
    string? ProgrammaticValue)
    : IOrderableEntity
{
    private const string CheckboxValueOn = "on";

    private readonly Lazy<VirtualOrderContainer<ProjectFieldVariant>> container
        = VirtualOrderContainerFacade.CreateLazy(Variants, Ordering);

    public IReadOnlyList<ProjectFieldVariant> SortedVariants => container.Value.OrderedItems;

    public ProjectFieldVariant? LastVariant => SortedVariants.LastOrDefault(x => x.IsActive);

    public string GetDisplayValue(string? value, IReadOnlyList<int>? selectedIDs)
    {
        if (Type == ProjectFieldType.Checkbox)
        {
            return value?.StartsWith(CheckboxValueOn) == true ? "☑️" : "☐";
        }

        if (HasValueList)
        {
            return Variants.Where(dv => selectedIDs!.Contains(dv.Id.ProjectFieldVariantId))
                    .Select(dv => dv.Label)
                    .JoinStrings(", ");
        }

        return value ?? "";
    }

    public bool HasValueList => Type.HasValuesList();
    public bool SupportsMassAdding => Type.SupportsMassAdding();
    public bool SupportsMarkdown => Type == ProjectFieldType.Text;
    public bool HasSpecialGroup => HasValueList && BoundTo == FieldBoundTo.Character;

    public bool CanHaveValue => Type != ProjectFieldType.Header;

    public bool SupportsPricing => Type.SupportsPricing();

    public bool SupportsPricingOnField => Type.SupportsPricingOnField();

    public bool HasPrice => SupportsPricing &&
               ((SupportsPricingOnField && Price != 0)
                || (!SupportsPricingOnField &&
                    Variants.Any(v => v.Price != 0)));

    public bool IsName { get; } = FieldSettings.NameField == Id;

    public bool IsDescription { get; } = FieldSettings.DescriptionField == Id;

    /// <summary>
    /// Special field - schedule time slot
    /// </summary>
    public bool IsTimeSlot => Type == ProjectFieldType.ScheduleTimeSlotField;
    /// <summary>
    /// Special field - schedule room slot
    /// </summary>
    public bool IsRoomSlot => Type == ProjectFieldType.ScheduleRoomField;

    public IEnumerable<ProjectFieldVariant> GetPossibleVariants(
        AccessArguments accessArguments,
        IReadOnlyCollection<int> selectedIds)
        => SortedVariants.Where(v =>
            selectedIds.Contains(v.Id.ProjectFieldVariantId) ||
            (v.IsActive && (v.IsPlayerSelectable || accessArguments.MasterAccess))
            );

    int IOrderableEntity.Id => Id.ProjectFieldId;

    public bool HasEditAccess(AccessArguments accessArguments)
    {
        return accessArguments.MasterAccess
               ||
               (accessArguments.PlayerAccessToCharacter && CanPlayerEdit &&
                BoundTo == FieldBoundTo.Character)
               ||
               (accessArguments.PlayerAccesToClaim && CanPlayerEdit &&
               (ShowOnUnApprovedClaims || accessArguments.PlayerAccessToCharacter));
    }

    public bool HasViewAccess(AccessArguments accessArguments)
    {
        return IsPublic
          || accessArguments.MasterAccess
          ||
          (accessArguments.PlayerAccessToCharacter && CanPlayerView &&
           BoundTo == FieldBoundTo.Character)
          ||
          (accessArguments.PlayerAccesToClaim && CanPlayerView &&
           BoundTo == FieldBoundTo.Claim);
    }
}
