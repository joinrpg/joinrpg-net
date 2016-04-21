using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class FieldValueViewModel
  {
    private int ProjectFieldId { get; }

    public ProjectFieldViewType FieldViewType { get; }
    public bool CanView { get; }
    public bool CanEdit { get; }

    public bool IsPlayerVisible { get; }

    public string Value { get; }
    public string FieldName { get; }

    public bool IsDeleted { get; }

    public MarkdownViewModel Description { get; }

    public string FieldClientId => $"{HtmlIdPrefix}{ProjectFieldId}";

    public IReadOnlyList<ProjectFieldDropdownValue> ValueList { get; }
    public IEnumerable<ProjectFieldDropdownValue> PossibleValueList { get; }
    public FieldValueViewModel(CustomFieldsViewModel model, FieldWithValue ch)
    {
      Value = ch.Value;
      FieldViewType = (ProjectFieldViewType)ch.Field.FieldType;
      FieldName = ch.Field.FieldName;
      Description = new MarkdownViewModel(ch.Field.Description);

      IsPlayerVisible = ch.Field.CanPlayerView;
      IsDeleted = !ch.Field.IsActive;

      CanView = ch.HasValue() && (
            ch.Field.IsPublic
            || model.HasMasterAccess
            || (model.HasPlayerAccessToCharacter && ch.Field.CanPlayerView && ch.Field.FieldBoundTo == FieldBoundTo.Character)
            || (model.HasPlayerClaimAccess && ch.Field.CanPlayerView && ch.Field.FieldBoundTo == FieldBoundTo.Claim)
            );

      CanEdit = (ch.Field.IsAvailableForTarget(model.Target) || ch.HasValue()) && model.EditAllowed && (
           model.HasMasterAccess
           || (model.HasPlayerAccessToCharacter && ch.Field.CanPlayerEdit && ch.Field.FieldBoundTo == FieldBoundTo.Character)
           || (model.HasPlayerClaimAccess && ch.Field.CanPlayerEdit && ch.Field.FieldBoundTo == FieldBoundTo.Claim)
           );

      if (ch.Field.HasValueList())
      {
        ValueList = ch.GetDropdownValues();
        PossibleValueList = ch.GetPossibleValues();
      }
      ProjectFieldId = ch.Field.ProjectFieldId;

      FieldBound =  (FieldBoundToViewModel) ch.Field.FieldBoundTo;
      MandatoryStatus = IsDeleted ? MandatoryStatusViewType.Optional : (MandatoryStatusViewType) ch.Field.MandatoryStatus;
    }

    public MandatoryStatusViewType MandatoryStatus { get; }

    public FieldBoundToViewModel FieldBound { get; }


    public const string HtmlIdPrefix = "field_";
  }

  public class CustomFieldsViewModel
  {
    private int? CurrentUserId { get; }
    public bool HasPlayerAccessToCharacter { get; }
    public bool HasPlayerClaimAccess { get; }
    public bool HasMasterAccess { get; }
    public bool EditAllowed { get; private set; } = true;
    public IClaimSource Target { get;  }
    private ICollection<FieldWithValue> FieldsWithValues { get; set; }

    public IEnumerable<FieldValueViewModel> Fields
    {
      get { return FieldsWithValues.Select(ch => new FieldValueViewModel(this, ch)); }
    }

    public CustomFieldsViewModel DisableEdit()
    {
      EditAllowed = false;
      return this;
    }

    public CustomFieldsViewModel(int? currentUserId, IClaimSource target)
    {
      CurrentUserId = currentUserId;
      HasMasterAccess = target.Project.HasMasterAccess(currentUserId);
      FieldsWithValues = target.Project.GetFields().ToList();

      Target = target;

      OnlyClaimFields();
      HasPlayerClaimAccess = true;
     
    }

    public CustomFieldsViewModel(int? currentUserId, Character character)
    {
      CurrentUserId = currentUserId;
      HasMasterAccess = character.HasMasterAccess(currentUserId);
      FieldsWithValues = character.Project.GetFields().ToList();
      Target = character;

      HasPlayerAccessToCharacter = character.HasPlayerAccess(CurrentUserId);
      HasPlayerClaimAccess = character.ApprovedClaim?.HasPlayerAccesToClaim(CurrentUserId) ?? false;
      FieldsWithValues.FillIfEnabled(character.ApprovedClaim, character, CurrentUserId);
      FieldsWithValues = FieldsWithValues.Where(f => f.Field.FieldBoundTo == FieldBoundTo.Character).ToList();
    }

    public CustomFieldsViewModel(int? currentUserId, Claim claim)
    {

      CurrentUserId = currentUserId;
      HasMasterAccess = claim.HasMasterAccess(currentUserId);
      FieldsWithValues = claim.Project.GetFields().ToList();
      Target = claim.GetTarget();

      HasPlayerClaimAccess = claim.HasPlayerAccesToClaim(CurrentUserId);
      HasPlayerAccessToCharacter = claim.Character != null && claim.Character.HasPlayerAccess(CurrentUserId);
      FieldsWithValues.FillIfEnabled(claim, claim.Character, CurrentUserId);
      if (!claim.IsApproved)
        OnlyClaimFields();
    }

    private void OnlyClaimFields()
    {
      FieldsWithValues = FieldsWithValues.Where(f => f.Field.FieldBoundTo == FieldBoundTo.Claim).ToList();
    }

    public bool AnythingAccessible => Fields.Any(f => f.CanEdit || f.CanView);
  }
}
