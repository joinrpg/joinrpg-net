using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Models
{
  public class FieldPossibleValueViewModel
  {
    public FieldPossibleValueViewModel(ProjectFieldDropdownValue value, bool hasPrice, bool selected = false)
    {
      ProjectFieldDropdownValueId = value.ProjectFieldDropdownValueId;
      DescriptionPlainText = value.Description.ToPlainText();
      Label = value.Label;
      DescriptionHtml = value.Description.ToHtmlString();
      MasterDescriptionHtml = value.MasterDescription.ToHtmlString();
      SpecialGroupId = value.CharacterGroup?.CharacterGroupId;
            Price = value.Price;
            HasPrice = hasPrice;
            Selected = selected;
    }

    public int? SpecialGroupId { get; }

    public int ProjectFieldDropdownValueId { get; }

    public IHtmlString DescriptionPlainText { get; }

    public string Label { get; }
    public IHtmlString DescriptionHtml { get; }
    public IHtmlString MasterDescriptionHtml { get; }

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
  //Actually most of this logic should be moved to Domain
  public class FieldValueViewModel
  {
    public int ProjectFieldId { get; }

    public ProjectFieldViewType FieldViewType { get; }
    public bool CanView { get; }
    public bool CanEdit { get; }

    public bool IsPlayerVisible { get; }

    public bool HasMasterAccess { get; }

    public string Value { get; }

    public bool HasValue { get; }

    public IHtmlString DisplayString { get; }
    public string FieldName { get; }

    public bool IsDeleted { get; }

    public IHtmlString Description { get; }

      public IHtmlString MasterDescription { get; }

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
          if (ch == null) throw new ArgumentNullException(nameof(ch));

          Value = ch.Value;

          DisplayString = ch.Field.SupportsMarkdown()
              ? new MarkdownString(ch.DisplayString).ToHtmlString(renderer)
              : ch.DisplayString.SanitizeHtml();
          FieldViewType = (ProjectFieldViewType) ch.Field.FieldType;
          FieldName = ch.Field.FieldName;

          HasMasterAccess = model.AccessArguments.MasterAccess;
          Description = ch.Field.Description.ToHtmlString();

          MasterDescription = HasMasterAccess ? ch.Field.MasterDescription.ToHtmlString() : MvcHtmlString.Empty;

          IsPlayerVisible = ch.Field.CanPlayerView;
          IsDeleted = !ch.Field.IsActive;

          HasValue = ch.HasViewableValue;

          CanView = ch.HasViewableValue
                    && ch.HasViewAccess(model.AccessArguments)
                    && (ch.HasEditableValue || ch.Field.IsAvailableForTarget(model.Target));

          CanEdit = model.EditAllowed
                    && ch.HasEditAccess(model.AccessArguments)
                    && (ch.HasEditableValue || ch.Field.IsAvailableForTarget(model.Target));


          // Detecting if field (or its values) has a price or not
          HasPrice = ch.SupportsPricing();

          //if not "HasValues" types, will be empty
          ValueList = ch.GetDropdownValues()
              .Select(v => new FieldPossibleValueViewModel(v, HasPrice, true)).ToList();
          PossibleValueList = ch.GetPossibleValues(model.AccessArguments).Select(v => new FieldPossibleValueViewModel(v,
                  HasPrice,
                  ValueList.Any(sv =>
                      sv.ProjectFieldDropdownValueId == v.ProjectFieldDropdownValueId)))
              .ToArray();

          if (HasPrice)
          {
              if (FieldViewType.SupportsPricingOnField())
                  Price = ch.Field.Price;
              Fee = ch.GetCurrentFee();
          }

          ShowPrice = HasPrice && model.AccessArguments.AnyAccessToClaim;

          ProjectFieldId = ch.Field.ProjectFieldId;

          FieldBound = (FieldBoundToViewModel) ch.Field.FieldBoundTo;
          MandatoryStatus = IsDeleted
              ? MandatoryStatusViewType.Optional
              : (MandatoryStatusViewType) ch.Field.MandatoryStatus;

          ProjectId = ch.Field.ProjectId;

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
        public IClaimSource Target { get; }

        public IReadOnlyCollection<FieldValueViewModel> Fields { get; }

        /// <summary>
        /// Sum of fields fees
        /// </summary>
        public readonly Dictionary<FieldBoundToViewModel, int> FieldsFee = new Dictionary<FieldBoundToViewModel, int>();

        /// <summary>
        /// Total number of fields with fee
        /// </summary>
        public readonly Dictionary<FieldBoundToViewModel, int> FieldWithFeeCount = new Dictionary<FieldBoundToViewModel, int>();

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
            foreach (FieldBoundToViewModel key in Enum.GetValues(typeof(FieldBoundToViewModel)))
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
        public CustomFieldsViewModel()
        {
            InitTotals();
        }

        /// <summary>
        /// Called from AddClaimViewModel
        /// </summary>
        public CustomFieldsViewModel(int? currentUserId, IClaimSource target) : this()
        {
            AccessArguments = new AccessArguments(
              target.HasMasterAccess(currentUserId),
              playerAccessToCharacter: false,
              playerAccesToClaim: true);

            EditAllowed = target.Project.Active;

            Target = target;

            var renderer = new JoinrpgMarkdownLinkRenderer(Target.Project);

            var fieldsList = target.Project.GetFieldsNotFilled();
            if (target is Character character)
            {
                fieldsList.FillIfEnabled(claim: null, character: character);
            }
            Fields =
              fieldsList
                .Select(ch => CreateFieldValueView(ch, renderer))
                .ToList();

            
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
        /// <param name="disableEdit">disable editing (incl. cases where it's done to speeds up the app)</param>
        /// <param name="onlyPlayerVisible">
        /// Used for printing, when the user who prints has master access,
        /// whereas the print result should contain only user-visible fields.
        /// </param>
        /// <param name="wherePrintEnabled">when true - print only fields where IncludeInPrint = true</param>
        public CustomFieldsViewModel(
          int? currentUserId,
          Character character,
          bool disableEdit = false,
          bool onlyPlayerVisible = false,
          bool wherePrintEnabled = false) : this()
        {
            EditAllowed = !disableEdit && character.Project.Active;
            if (onlyPlayerVisible)
            {
                AccessArguments = new AccessArguments(
                  masterAccess: false,
                  //TODO: this printing code might do smth wrong. Why Any access if we need palyer visible only?
                  playerAccessToCharacter: character.HasAnyAccess(currentUserId),
                  playerAccesToClaim: character.ApprovedClaim?.HasAccess(currentUserId, ExtraAccessReason.Player) ?? false);
            }
            else
            {
                AccessArguments = new AccessArguments(character, currentUserId);
            }

            Target = character;
            var joinrpgMarkdownLinkRenderer = new JoinrpgMarkdownLinkRenderer(Target.Project);
            Fields =
              character.Project.GetFieldsNotFilled()
                .Where(f => f.Field.FieldBoundTo == FieldBoundTo.Character)
                .Where(f => !wherePrintEnabled || f.Field.IncludeInPrint)
                .ToList()
                .FillIfEnabled(character.ApprovedClaim, character)
                .Select(ch => CreateFieldValueView(ch, joinrpgMarkdownLinkRenderer))
                .ToArray();
        }

        /// <summary>
        /// Called from Claim and Claim list
        /// </summary>
        public CustomFieldsViewModel(int? currentUserId, Claim claim) : this()
        {
            AccessArguments = new AccessArguments(claim, currentUserId);

            Target = claim.GetTarget();
            EditAllowed = claim.Project.Active;

            var renderer = new JoinrpgMarkdownLinkRenderer(Target.Project);

            Fields =
              claim.Project.GetFieldsNotFilled()
                .ToList()
                .FillIfEnabled(claim, claim.IsApproved ? claim.Character : null)
                .Select(ch => CreateFieldValueView(ch, renderer))
                .ToArray();
        }

        /// <summary>
        /// Creates field value view object
        /// </summary>
        private FieldValueViewModel CreateFieldValueView(FieldWithValue fv, ILinkRenderer renderer)
        {
            FieldValueViewModel result = new FieldValueViewModel(this, fv, renderer);
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

    [CanBeNull]
    public FieldValueViewModel FieldById(int projectFieldId)
    {
      return Fields.SingleOrDefault(field => field.ProjectFieldId == projectFieldId);
    }
        [CanBeNull]
        public FieldValueViewModel Field(ProjectField field)
        {
            return FieldById(field.ProjectFieldId);
        }
    }
}
