using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Models
{
  //Actually most of this logic should be moved to Domain
  public class FieldValueViewModel
  {
    public int ProjectFieldId { get; }

    public ProjectFieldViewType FieldViewType { get; }
    public bool CanView { get; }
    public bool CanEdit { get; }

    public bool IsPlayerVisible { get; }

    public string Value { get; }

    public bool HasValue { get; }

    public IHtmlString DisplayString { get; }
    public string FieldName { get; }

    public bool IsDeleted { get; }

    public IHtmlString Description { get; }

    public string FieldClientId => $"{HtmlIdPrefix}{ProjectFieldId}";

    public IReadOnlyList<ProjectFieldDropdownValue> ValueList { get; }
    public IEnumerable<ProjectFieldDropdownValue> PossibleValueList { get; }
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
      FieldViewType = (ProjectFieldViewType)ch.Field.FieldType;
      FieldName = ch.Field.FieldName;
      Description = ch.Field.Description.ToHtmlString();

      IsPlayerVisible = ch.Field.CanPlayerView;
      IsDeleted = !ch.Field.IsActive;

      HasValue = ch.HasViewableValue;

      var hasViewAccess = ch.HasViewableValue
        && ch.HasViewAccess(
          model.HasMasterAccess,
          model.HasPlayerAccessToCharacter,
          model.HasPlayerClaimAccess);

      CanView = hasViewAccess && ch.HasViewableValue;

      CanEdit = model.EditAllowed 
          && ch.HasEditAccess(
            model.HasMasterAccess, 
            model.HasPlayerAccessToCharacter,
            model.HasPlayerClaimAccess, 
            model.Target)
            && (ch.HasEditableValue || ch.Field.IsAvailableForTarget(model.Target));

      if (ch.Field.HasValueList())
      {
        ValueList = ch.GetDropdownValues().ToArray();
        PossibleValueList = ch.GetPossibleValues();
      }
      ProjectFieldId = ch.Field.ProjectFieldId;

      FieldBound =  (FieldBoundToViewModel) ch.Field.FieldBoundTo;
      MandatoryStatus = IsDeleted ? MandatoryStatusViewType.Optional : (MandatoryStatusViewType) ch.Field.MandatoryStatus;
    }

    public MandatoryStatusViewType MandatoryStatus { get; }

    public FieldBoundToViewModel FieldBound { get; }


    public const string HtmlIdPrefix = "field_";

    /// <summary>
    /// Value for checkbox filelds only
    /// </summary>
    public bool IsCheckboxSet() => !string.IsNullOrWhiteSpace(Value);
  }

  public class CustomFieldsViewModel
  {
    private int? CurrentUserId { get; }
    public bool HasPlayerAccessToCharacter { get; }
    public bool HasPlayerClaimAccess { get; }
    public bool HasMasterAccess { get; }
    public bool EditAllowed { get; } 
    public IClaimSource Target { get;  }

    public ICollection<FieldValueViewModel> Fields { get; }

    /// <summary>
    /// Called from AddClaimViewModel
    /// </summary>
    public CustomFieldsViewModel(int? currentUserId, IClaimSource target)
    {
      CurrentUserId = currentUserId;
      HasMasterAccess = target.HasMasterAccess(currentUserId);
      EditAllowed = target.Project.Active;

      Target = target;

      HasPlayerClaimAccess = true;
      var renderer = new JoinrpgMarkdownLinkRenderer(Target.Project);

      Fields =
        target.Project.GetFieldsNotFilled()
          .Select(ch => new FieldValueViewModel(this, ch, renderer))
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
    /// <param name="disableEdit">disable editing (incl. cases where it's done to speeds up the app)</param>
    /// <param name="onlyPlayerVisible">
    /// Used for printing, when the user who prints has master access,
    /// whereas the print result should contain only user-visible fields.
    /// </param>
    public CustomFieldsViewModel(
      int? currentUserId,
      Character character,
      bool disableEdit = false,
      bool onlyPlayerVisible = false,
      bool wherePrintEnabled = false)
    {
      EditAllowed = !disableEdit && character.Project.Active;
      CurrentUserId = currentUserId;
      if (onlyPlayerVisible)
      {
        HasMasterAccess = false;
        HasPlayerAccessToCharacter = character.HasAnyAccess(currentUserId);
        HasPlayerClaimAccess = character.ApprovedClaim?.HasAnyAccess(currentUserId) ?? false;
      }
      else
      {
        HasMasterAccess = character.HasMasterAccess(currentUserId);
        HasPlayerAccessToCharacter = character.HasPlayerAccess(CurrentUserId);
        HasPlayerClaimAccess = character.ApprovedClaim?.HasPlayerAccesToClaim(CurrentUserId) ?? false;
      }

      Target = character;
      var joinrpgMarkdownLinkRenderer = new JoinrpgMarkdownLinkRenderer(Target.Project);
      Fields =
        character.Project.GetFieldsNotFilled()
          .Where(f => f.Field.FieldBoundTo == FieldBoundTo.Character && (!wherePrintEnabled || f.Field.IncludeInPrint))
          .ToList()
          .FillIfEnabled(character.ApprovedClaim, character)
          .Select(ch => new FieldValueViewModel(this, ch, joinrpgMarkdownLinkRenderer))
          .ToArray();
    }

    /// <summary>
    /// Called from Claim and Claim list
    /// </summary>
    public CustomFieldsViewModel(int? currentUserId, Claim claim)
    {
      CurrentUserId = currentUserId;
      HasMasterAccess = claim.HasMasterAccess(currentUserId);
      Target = claim.GetTarget();
      EditAllowed = claim.Project.Active;

      HasPlayerClaimAccess = claim.HasPlayerAccesToClaim(CurrentUserId);
      HasPlayerAccessToCharacter = claim.Character != null && claim.Character.HasPlayerAccess(CurrentUserId);

      var renderer = new JoinrpgMarkdownLinkRenderer(Target.Project);

      Fields =
        claim.Project.GetFieldsNotFilled()
          .ToList()
          .FillIfEnabled(claim, claim.IsApproved ? claim.Character : null)
          .Select(ch => new FieldValueViewModel(this, ch, renderer))
          .ToArray();
    }

    public bool AnythingAccessible => Fields.Any(f => f.CanEdit || f.CanView);

    [CanBeNull]
    public FieldValueViewModel FieldById(int projectFieldId)
    {
      return Fields.SingleOrDefault(field => field.ProjectFieldId == projectFieldId);
    }
  }
}
