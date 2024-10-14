using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Models;

public class FieldPossibleValueViewModel
{
    public FieldPossibleValueViewModel(ProjectFieldVariant value, bool hasPrice, bool selected = false)
    {
        ProjectFieldDropdownValueId = value.Id.ProjectFieldVariantId;
        DescriptionPlainText = value.Description.ToPlainText();
        Label = value.Label;
        DescriptionHtml = value.Description.ToHtmlString();
        MasterDescriptionHtml = value.MasterDescription.ToHtmlString();
        SpecialGroupId = value.CharacterGroupId;
        Price = value.Price;
        HasPrice = hasPrice;
        Selected = selected;
    }

    public int? SpecialGroupId { get; }

    public int ProjectFieldDropdownValueId { get; }

    public JoinHtmlString DescriptionPlainText { get; }

    public string Label { get; }
    public JoinHtmlString DescriptionHtml { get; }
    public JoinHtmlString MasterDescriptionHtml { get; }

    /// <summary>
    /// Value's price as specified in value's definition
    /// </summary>
    public int Price { get; }

    /// <summary>
    /// True if owner has a price
    /// </summary>
    public bool HasPrice { get; }

    public bool Selected { get; }
}

public enum FieldSpecialLabelView
{
    ForClaim,
    Name,
    Description,
    ScheduleTime,
    SchedulePlace,
}

//Actually most of this logic should be moved to Domain
public class FieldValueViewModel
{
    public int ProjectFieldId { get; }

    public List<FieldSpecialLabelView> Labels { get; } = [];

    public ProjectFieldViewType FieldViewType { get; }
    public bool CanView { get; }
    public bool CanEdit { get; }

    public bool IsPlayerVisible { get; }

    public bool HasMasterAccess { get; }

    public string? Value { get; }

    public bool HasValue { get; }

    public JoinHtmlString DisplayString { get; }
    public string FieldName { get; }

    public bool IsDeleted { get; }

    public JoinHtmlString Description { get; }

    public JoinHtmlString MasterDescription { get; }

    /// <summary>
    /// Field's price as specified in field's definition
    /// </summary>
    public int Price { get; }

    /// <summary>
    /// Returns true if a field supports price and has it
    /// </summary>
    public bool HasPrice { get; }

    /// <summary>
    /// true if price information should be visible
    /// </summary>
    public bool ShowPrice { get; }

    /// <summary>
    /// Actual fee has to be paid by the player
    /// </summary>
    public int Fee { get; }

    public string FieldClientId => $"{HtmlIdPrefix}{ProjectFieldId}";
    [NotNull]
    public IReadOnlyList<FieldPossibleValueViewModel> ValueList { get; }
    [NotNull]
    public IReadOnlyList<FieldPossibleValueViewModel> PossibleValueList { get; }

    public FieldValueViewModel(
        CustomFieldsViewModel model,
        [NotNull] FieldWithValue ch,
        ILinkRenderer renderer)
    {
        if (ch == null)
        {
            throw new ArgumentNullException(nameof(ch));
        }

        Value = ch.Value;

        DisplayString = ch.Field.SupportsMarkdown
            ? new MarkdownString(ch.DisplayString).ToHtmlString(renderer)
            : ch.DisplayString.SanitizeHtml();
        FieldViewType = (ProjectFieldViewType)ch.Field.Type;
        FieldName = ch.Field.Name;

        HasMasterAccess = model.AccessArguments.MasterAccess;
        Description = ch.Field.Description.ToHtmlString();

        MasterDescription = HasMasterAccess ? ch.Field.MasterDescription.ToHtmlString() : "".MarkAsHtmlString();

        IsPlayerVisible = ch.Field.CanPlayerView;
        IsDeleted = !ch.Field.IsActive;

        HasValue = ch.HasViewableValue;

        CanView = ch.HasViewableValue
                  && ch.Field.HasViewAccess(model.AccessArguments)
                  && (ch.HasEditableValue || ch.Field.IsAvailableForTarget(model.Target));

        CanEdit = model.EditAllowed
                  && ch.Field.HasEditAccess(model.AccessArguments)
                  && (ch.HasEditableValue || ch.Field.IsAvailableForTarget(model.Target));


        // Detecting if field (or its values) has a price or not
        HasPrice = ch.Field.HasPrice;

        //if not "HasValues" types, will be empty
        ValueList = ch.GetDropdownValues()
            .Select(v => new FieldPossibleValueViewModel(v, HasPrice, true)).ToList();
        PossibleValueList = ch.GetPossibleVariantsWithSelection(model.AccessArguments)
            .Select(pair => new FieldPossibleValueViewModel(pair.variant, HasPrice, pair.selected))
            .ToArray();

        if (HasPrice)
        {
            if (ch.Field.SupportsPricingOnField)
            {
                Price = ch.Field.Price;
            }

            Fee = ch.GetCurrentFee();
        }

        ShowPrice = HasPrice && model.AccessArguments.AnyAccessToClaim;

        ProjectFieldId = ch.Field.Id.ProjectFieldId;

        FieldBound = (FieldBoundToViewModel)ch.Field.BoundTo;
        MandatoryStatus = IsDeleted
            ? MandatoryStatusViewType.Optional
            : (MandatoryStatusViewType)ch.Field.MandatoryStatus;

        ProjectId = ch.Field.Id.ProjectId;

        SetFieldLabels(ch);

    }

    private void SetFieldLabels(FieldWithValue ch)
    {
        void AddLabelIf(FieldSpecialLabelView label, bool predicate)
        {
            if (predicate)
            {
                Labels.Add(label);
            }
        }

        AddLabelIf(FieldSpecialLabelView.ForClaim, ch.Field.BoundTo == FieldBoundTo.Claim);

        AddLabelIf(FieldSpecialLabelView.Name, ch.Field.IsName);
        AddLabelIf(FieldSpecialLabelView.Description, ch.Field.IsDescription);
        AddLabelIf(FieldSpecialLabelView.ScheduleTime, ch.Field.IsTimeSlot);
        AddLabelIf(FieldSpecialLabelView.SchedulePlace, ch.Field.IsRoomSlot);
    }

    public MandatoryStatusViewType MandatoryStatus { get; }

    public FieldBoundToViewModel FieldBound { get; }
    public int ProjectId { get; }

    public const string HtmlIdPrefix = "field_";

    /// <summary>
    /// Value for checkbox filelds only
    /// </summary>
    public bool IsCheckboxSet() => !string.IsNullOrWhiteSpace(Value);
}

public class CustomFieldsViewModel
{
    public AccessArguments AccessArguments { get; }
    public bool EditAllowed { get; }
    [Editable(false)]
    public IClaimSource Target { get; }

    [Editable(false)]
    public IReadOnlyCollection<FieldValueViewModel> Fields { get; }

    /// <summary>
    /// Sum of fields fees
    /// </summary>
    public readonly Dictionary<FieldBoundToViewModel, int> FieldsFee = [];

    /// <summary>
    /// Total number of fields with fee
    /// </summary>
    public readonly Dictionary<FieldBoundToViewModel, int> FieldWithFeeCount = [];

    /// <summary>
    /// Returns true if there is at least one field with fee
    /// </summary>
    public bool HasFieldsWithFee { get; private set; }

    /// <summary>
    /// Returns true if fields subtotal row should be shown
    /// </summary>
    public bool ShowFieldsSubtotal
        => HasFieldsWithFee && AccessArguments.AnyAccessToCharacter;

    /// <summary>
    /// Initializes dictionaries
    /// </summary>
    private void InitTotals()
    {
        foreach (var key in Enum.GetValues<FieldBoundToViewModel>())
        {
            FieldsFee[key] = 0;
            FieldWithFeeCount[key] = 0;
        }
    }

    /// <summary>
    /// Returns sum of fees of all fields
    /// </summary>
    public int FieldsTotalFee => FieldsFee.Sum(kv => kv.Value);

    /// <summary>
    /// Common constructor
    /// </summary>
    public CustomFieldsViewModel() => InitTotals();

    /// <summary>
    /// Called from AddClaimViewModel
    /// </summary>
    public CustomFieldsViewModel(int? currentUserId, IClaimSource target, ProjectInfo projectInfo, Dictionary<int, string?>? overrideValues = null) : this()
    {
        AccessArguments = new AccessArguments(
          target.HasMasterAccess(currentUserId),
          PlayerAccessToCharacter: false,
          PlayerAccesToClaim: true);

        EditAllowed = target.Project.Active;

        Target = target;

        var renderer = new JoinrpgMarkdownLinkRenderer(Target.Project);
        var fieldsList = target.GetFieldsForClaimSource(projectInfo);
        Fields =
          fieldsList
            .Select(ch => CreateFieldValueView(TryOverrideValue(ch), renderer))
            .ToList();

        FieldWithValue TryOverrideValue(FieldWithValue ch)
        {
            var overrideValue = overrideValues?.GetValueOrDefault(ch.Field.Id.ProjectFieldId);
            if (overrideValue is not null)
            {
                ch.Value = overrideValue;
            }
            return ch;
        }
    }

    /// <summary>
    ///  Called from
    /// - Character details
    /// - character list item
    /// - Edit character
    /// - print character
    /// </summary>
    /// <param name="currentUserId">ID of the currect user logged in</param>
    /// <param name="character">Character to print</param>
    /// <param name="projectInfo"></param>
    /// <param name="disableEdit">disable editing (incl. cases where it's done to speeds up the app)</param>
    /// <param name="onlyPlayerVisible">
    /// Used for printing, when the user who prints has master access,
    /// whereas the print result should contain only user-visible fields.
    /// </param>
    /// <param name="wherePrintEnabled">when true - print only fields where IncludeInPrint = true</param>
    public CustomFieldsViewModel(
      int? currentUserId,
      Character character,
      ProjectInfo projectInfo,
      bool disableEdit = false,
      bool onlyPlayerVisible = false, bool wherePrintEnabled = false) : this()
    {
        EditAllowed = !disableEdit && character.Project.Active;
        if (onlyPlayerVisible)
        {
            AccessArguments = new AccessArguments(
              MasterAccess: false,
              // Not a "player visible", because it could be master that asks to view as player
              PlayerAccessToCharacter: character.HasAnyAccess(currentUserId),
              PlayerAccesToClaim: character.ApprovedClaim?.HasAccess(currentUserId, ExtraAccessReason.Player) ?? false);
        }
        else
        {
            AccessArguments = AccessArgumentsFactory.Create(character, currentUserId);
        }

        Target = character;
        var joinrpgMarkdownLinkRenderer = new JoinrpgMarkdownLinkRenderer(Target.Project);
        Fields =
          character.GetFields(projectInfo)
            .Where(f => f.Field.BoundTo == FieldBoundTo.Character)
            .Where(f => !wherePrintEnabled || f.Field.IncludeInPrint)
            .Select(ch => CreateFieldValueView(ch, joinrpgMarkdownLinkRenderer))
            .ToArray();
    }

    /// <summary>
    /// Called from Claim and Claim list
    /// </summary>
    public CustomFieldsViewModel(int? currentUserId, Claim claim, ProjectInfo projectInfo) : this()
    {
        AccessArguments = AccessArgumentsFactory.Create(claim, currentUserId);

        Target = claim.GetTarget();
        EditAllowed = claim.Project.Active;

        var renderer = new JoinrpgMarkdownLinkRenderer(Target.Project);

        Fields =
          claim.GetFields(projectInfo)
            .Select(ch => CreateFieldValueView(ch, renderer))
            .ToArray();
    }

    /// <summary>
    /// Creates field value view object
    /// </summary>
    private FieldValueViewModel CreateFieldValueView(FieldWithValue fv, ILinkRenderer renderer)
    {
        var result = new FieldValueViewModel(this, fv, renderer);
        // Here is the point to calculate total fee
        if (result.HasPrice)
        {
            FieldsFee[result.FieldBound] += result.Fee;
            FieldWithFeeCount[result.FieldBound]++;
            HasFieldsWithFee = true;
        }
        return result;
    }

    public bool AnythingAccessible => Fields.Any(f => f.CanEdit || f.CanView);

    public FieldValueViewModel? FieldById(int projectFieldId) => Fields.SingleOrDefault(field => field.ProjectFieldId == projectFieldId);
    public FieldValueViewModel? Field(ProjectField field) => FieldById(field.ProjectFieldId);

    public FieldValueViewModel? Field(ProjectFieldInfo field) => FieldById(field.Id.ProjectFieldId);
}
