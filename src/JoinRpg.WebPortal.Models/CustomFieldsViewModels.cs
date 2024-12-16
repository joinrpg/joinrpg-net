using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.Access;
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
    public IReadOnlyList<FieldPossibleValueViewModel> ValueList { get; }
    public IReadOnlyList<FieldPossibleValueViewModel> PossibleValueList { get; }

    public FieldValueViewModel(
        CustomFieldsViewModel model,
        FieldWithValue ch,
        ILinkRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(ch);

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

        CanEdit = model.AccessArguments.EditAllowed
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
    [Editable(false)]
    public Character Target { get; }

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
    /// Returns sum of fees of all fields
    /// </summary>
    public int FieldsTotalFee => FieldsFee.Sum(kv => kv.Value);

    /// <summary>
    /// Called from AddClaimViewModel
    /// </summary>
    public CustomFieldsViewModel(Character target, ProjectInfo projectInfo, AccessArguments accessArguments, Dictionary<int, string?>? overrideValues)
        : this(accessArguments, target, target.GetFields(projectInfo), overrideValues, projectInfo)
    {
    }

    /// <summary>
    ///  Called from
    /// - Character details
    /// - character list item
    /// - Edit character
    /// - print character
    /// </summary>
    /// <param name="character">Character to print</param>
    /// <param name="projectInfo"></param>
    /// <param name="accessArguments"></param>
    /// <param name="wherePrintEnabled">when true - print only fields where IncludeInPrint = true</param>
    /// <param name="overrideValues"></param>
    public CustomFieldsViewModel(
      Character character,
      ProjectInfo projectInfo,
      AccessArguments accessArguments,
      bool wherePrintEnabled = false,
      Dictionary<int, string?>? overrideValues = null)
        : this(
              accessArguments,
              character,
              character.GetFields(projectInfo).Where(f => f.Field.BoundTo == FieldBoundTo.Character).Where(f => !wherePrintEnabled || f.Field.IncludeInPrint),
              overrideValues,
              projectInfo)
    {
    }

    /// <summary>
    /// Called from Claim and Claim list
    /// </summary>
    public CustomFieldsViewModel(int? currentUserId, Claim claim, ProjectInfo projectInfo)
      : this(AccessArgumentsFactory.Create(claim, currentUserId), claim.Character, claim.GetFields(projectInfo), overrideValues: null, projectInfo)
    {
    }

    /// <summary>
    /// Common constructor
    /// </summary>
    private CustomFieldsViewModel(
        AccessArguments accessArguments,
        Character target,
        IEnumerable<FieldWithValue> fields,
        Dictionary<int, string?>? overrideValues,
        ProjectInfo projectInfo
        )
    {
        foreach (var key in Enum.GetValues<FieldBoundToViewModel>())
        {
            FieldsFee[key] = 0;
            FieldWithFeeCount[key] = 0;
        }
        AccessArguments = accessArguments;
        Target = target;
        var renderer = new JoinrpgMarkdownLinkRenderer(Target.Project, projectInfo);
        Fields = fields.Select(ch => CreateFieldValueView(ch, renderer, overrideValues)).ToList();
    }

    /// <summary>
    /// Creates field value view object
    /// </summary>
    private FieldValueViewModel CreateFieldValueView(FieldWithValue fv, ILinkRenderer renderer, Dictionary<int, string?>? overrideValues)
    {
        var result = new FieldValueViewModel(this, TryOverrideValue(fv), renderer);
        // Here is the point to calculate total fee
        if (result.HasPrice)
        {
            FieldsFee[result.FieldBound] += result.Fee;
            FieldWithFeeCount[result.FieldBound]++;
            HasFieldsWithFee = true;
        }
        return result;

        FieldWithValue TryOverrideValue(FieldWithValue ch)
        {
            if (overrideValues?.GetValueOrDefault(ch.Field.Id.ProjectFieldId) is string overrideValue)
            {
                ch.Value = overrideValue;
            }
            return ch;
        }
    }

    public bool AnythingAccessible => Fields.Any(f => f.CanEdit || f.CanView);

    public FieldValueViewModel? Field(ProjectFieldInfo field) => Fields.SingleOrDefault(f => f.ProjectFieldId == field.Id.ProjectFieldId);
}
