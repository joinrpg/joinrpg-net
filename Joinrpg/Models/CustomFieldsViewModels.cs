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

    public string Value { get; }
    public string FieldName { get; }

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

      CanView = !string.IsNullOrWhiteSpace(ch.Value) && (
            ch.Field.IsPublic
            || model.HasMasterAccess
            || (model.HasPlayerAccessToCharacter && ch.Field.CanPlayerView && ch.Field.FieldBoundTo == FieldBoundTo.Character)
            || (model.HasPlayerClaimAccess && ch.Field.CanPlayerView && ch.Field.FieldBoundTo == FieldBoundTo.Claim)
            );
      CanEdit = (ch.Field.IsActive || !string.IsNullOrWhiteSpace(ch.Value)) && model.EditAllowed && (
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
    }


    public const string HtmlIdPrefix = "field_";
  }

  public class CustomFieldsViewModel
  {
    private int? CurrentUserId { get; }
    public bool HasPlayerAccessToCharacter { get; private set; }
    public bool HasPlayerClaimAccess { get; private set; }
    public bool HasMasterAccess { get; }
    public bool EditAllowed { get; private set; } = true;
    private ICollection<FieldWithValue> FieldsWithValues { get; set; }

    public IEnumerable<FieldValueViewModel> Fields
    {
      get { return FieldsWithValues.Select(ch => new FieldValueViewModel(this, ch)); }
    }

    public bool AnyFieldEditable
      => EditAllowed && (HasMasterAccess || FieldsWithValues.Any(field => field.Field.CanPlayerEdit));

    public CustomFieldsViewModel DisableEdit()
    {
      EditAllowed = false;
      return this;
    }

    public CustomFieldsViewModel(int? currentUserId, Project project)
    {
      CurrentUserId = currentUserId;
      HasMasterAccess = project.HasMasterAccess(currentUserId);
      FieldsWithValues = project.GetFields().ToList();
    }

    public CustomFieldsViewModel FillFromClaim(Claim claim)
    {
      HasPlayerClaimAccess = claim.HasPlayerAccesToClaim(CurrentUserId);
      HasPlayerAccessToCharacter = claim.Character != null && claim.Character.HasPlayerAccess(CurrentUserId);
      FieldsWithValues.FillIfEnabled(claim, claim.Character, CurrentUserId);
      return this;
    }

    public CustomFieldsViewModel FillFromCharacter(Character character)
    {
      HasPlayerAccessToCharacter = character.HasPlayerAccess(CurrentUserId);
      HasPlayerClaimAccess = character.ApprovedClaim?.HasPlayerAccesToClaim(CurrentUserId) ?? false;
      FieldsWithValues.FillIfEnabled(character.ApprovedClaim, character, CurrentUserId);
      return this;
    }

    public CustomFieldsViewModel OnlyCharacterFields()
    {
      FieldsWithValues = FieldsWithValues.Where(f => f.Field.FieldBoundTo == FieldBoundTo.Character).ToList();
      return this;
    }

    public CustomFieldsViewModel OnlyClaimFields()
    {
      FieldsWithValues = FieldsWithValues.Where(f => f.Field.FieldBoundTo == FieldBoundTo.Claim).ToList();
      return this;
    }

    public CustomFieldsViewModel EnableClaimAccess()
    {
      HasPlayerClaimAccess = true;
      return this;
    }
  }
}
