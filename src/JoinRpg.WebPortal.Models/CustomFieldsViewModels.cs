using System.Text.Encodings.Web;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectMasterTools.Fields;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models;

public class FieldPossibleValueViewModel(ProjectFieldVariant value, bool hasPrice, bool selected = false)
{
    public int? SpecialGroupId { get; } = value.CharacterGroupId?.CharacterGroupId;

    public int ProjectFieldDropdownValueId { get; } = value.Id.ProjectFieldVariantId;

    public string Label { get; } = value.Label;
    public JoinHtmlString DescriptionHtml { get; } = value.Description.ToHtmlString();
    public JoinHtmlString MasterDescriptionHtml { get; } = value.MasterDescription.ToHtmlString();

    /// <summary>
    /// Value's price as specified in value's definition
    /// </summary>
    public int Price { get; } = value.Price;

    /// <summary>
    /// True if owner has a price
    /// </summary>
    public bool HasPrice { get; } = hasPrice;

    public bool Selected { get; } = selected;
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

    /// <summary>
    /// Ссылки на пользователей для полей типа <see cref="ProjectFieldViewType.UserLink"/> (ADR017).
    /// У остальных типов полей пуст.
    /// </summary>
    public IReadOnlyList<UserLinkViewModel> UserLinks { get; }

    public FieldValueViewModel(
        CustomFieldsViewModel model,
        FieldWithValue ch,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users)
    {
        ArgumentNullException.ThrowIfNull(ch);

        Value = ch.Value;

        // Директивы вида %персонаж123/%контакты123/%список45 в значениях полей НЕ разворачиваются:
        // рендерер тут не передаётся, и markdown рендерится с DoNothingLinkRenderer. Это работает
        // только во вводных — см. docs/plot/special.rst в joinrpg-docs.
        DisplayString = ch.Field.SupportsMarkdown
            ? new MarkdownString(ch.DisplayString).ToHtmlString()
            : new MarkupString(HtmlEncoder.Default.Encode(ch.DisplayString));
        FieldViewType = (ProjectFieldViewType)ch.Field.Type;
        FieldName = ch.Field.Name;

        HasMasterAccess = model.AccessArguments.MasterAccess;
        Description = ch.Field.Description.ToHtmlString();

        MasterDescription = HasMasterAccess ? ch.Field.MasterDescription.ToHtmlString() : new MarkupString();

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

        // Пользователь, которого нет в словаре, удалён (или id в значении — мусор):
        // ссылки не будет, но запись поля из-за этого не пропадает.
        // Здесь нельзя collection expression ([.. ...]): для IReadOnlyList<T> компилятор создаёт
        // внутренний тип <>z__ReadOnlyList<T> в этой сборке, а список уезжает параметром
        // InitialUsers в WASM-остров JoinUserLinkEditor. Параметры острова сериализуются вместе с
        // именем рантайм-типа, и клиент такой тип найти не может — остров падает на старте
        // («could not be found»), страница заявки ломается.
        UserLinks = ch.UserIds.Select(userId =>
            users.TryGetValue(userId, out var user)
                ? new UserLinkViewModel(user)
                : UserLinkViewModel.Deleted).ToList();

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
        AddLabelIf(FieldSpecialLabelView.ScheduleAuthor, ch.Field.IsScheduleAuthor);
        AddLabelIf(FieldSpecialLabelView.Public, ch.Field.IsPublic);
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

    /// <summary>
    /// Персонаж, по которому считается доступность полей. Интерфейс, а не EF-сущность: та же
    /// вьюмодель обслуживает и доменный агрегат <see cref="CharacterInfo"/> (ADR013).
    /// </summary>
    [Editable(false)]
    public IFieldAvailabilityTarget Target { get; }

    /// <summary>
    /// Нужен, чтобы доступность поля считалась по метаданным проекта, а не обходом ленивых
    /// EF-навигаций <see cref="Character"/>.
    /// </summary>
    [Editable(false)]
    public ProjectInfo ProjectInfo { get; }

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
    public CustomFieldsViewModel(
        Character target,
        ProjectInfo projectInfo,
        AccessArguments accessArguments,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users,
        Dictionary<int, string?>? overrideValues)
        : this(accessArguments, AvailabilityTarget(target, projectInfo), target.GetFields(projectInfo), overrideValues, projectInfo, users)
    {
    }

    /// <summary>
    /// Поля персонажа поверх доменного агрегата (ADR013) — страница подачи заявки.
    /// </summary>
    public CustomFieldsViewModel(
        CharacterInfo character,
        AccessArguments accessArguments,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users,
        Dictionary<int, string?>? overrideValues = null)
        : this(accessArguments, character, character.GetAllFields(), overrideValues, character.ProjectInfo, users)
    {
    }

    /// <summary>
    /// Поля персонажа для печати поверх доменного агрегата (ADR013): только character-bound и только
    /// те, что помечены «включать в распечатку».
    /// </summary>
    public static CustomFieldsViewModel ForPrint(
        CharacterInfo character,
        AccessArguments accessArguments,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users)
        => new(accessArguments,
            character,
            character.GetAllFields().Where(f => f.Field.BoundTo == FieldBoundTo.Character && f.Field.IncludeInPrint),
            overrideValues: null,
            character.ProjectInfo,
            users);

    /// <summary>
    ///  Called from
    /// - Character details
    /// - character list item
    /// - Edit character
    /// </summary>
    /// <param name="character">Character to show</param>
    /// <param name="projectInfo"></param>
    /// <param name="accessArguments"></param>
    public CustomFieldsViewModel(
      Character character,
      ProjectInfo projectInfo,
      AccessArguments accessArguments,
      IReadOnlyDictionary<UserIdentification, UserInfoHeader> users)
        : this(
              accessArguments,
              AvailabilityTarget(character, projectInfo),
              character.GetFields(projectInfo).Where(f => f.Field.BoundTo == FieldBoundTo.Character),
              overrideValues: null,
              projectInfo,
              users)
    {
    }

    /// <summary>
    /// Called from Claim and Claim list
    /// </summary>
    public CustomFieldsViewModel(
        int? currentUserId,
        Claim claim,
        ProjectInfo projectInfo,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users)
      : this(
            AccessArgumentsFactory.Create(claim, currentUserId, projectInfo),
            AvailabilityTarget(claim.Character, projectInfo),
            claim.GetFields(projectInfo),
            overrideValues: null,
            projectInfo,
            users)
    {
    }

    private static CharacterItem AvailabilityTarget(Character character, ProjectInfo projectInfo)
        => new(character, [.. character.GetParentGroupIdsToTop(projectInfo)]);

    /// <summary>
    /// Common constructor
    /// </summary>
    private CustomFieldsViewModel(
        AccessArguments accessArguments,
        IFieldAvailabilityTarget target,
        IEnumerable<FieldWithValue> fields,
        Dictionary<int, string?>? overrideValues,
        ProjectInfo projectInfo,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users
        )
    {
        foreach (var key in Enum.GetValues<FieldBoundToViewModel>())
        {
            FieldsFee[key] = 0;
            FieldWithFeeCount[key] = 0;
        }
        AccessArguments = accessArguments;
        Target = target;
        ProjectInfo = projectInfo;
        Fields = fields.Select(ch => CreateFieldValueView(ch, overrideValues, users)).ToList();
    }

    /// <summary>
    /// Creates field value view object
    /// </summary>
    private FieldValueViewModel CreateFieldValueView(
        FieldWithValue fv,
        Dictionary<int, string?>? overrideValues,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users)
    {
        var result = new FieldValueViewModel(this, TryOverrideValue(fv), users);
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
