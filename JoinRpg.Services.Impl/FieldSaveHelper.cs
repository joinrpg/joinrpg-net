using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.Services.Impl
{
  internal static class FieldSaveHelper
  {
    public static void SaveCharacterFieldsImpl(int currentUserId, Character character, Claim claim, IDictionary<int, string> newFieldValue)
    {
      var project = character?.Project ?? claim?.Project;
      var fields = project.GetFields().ToList().FillIfEnabled(claim, character, currentUserId).ToDictionary(f => f.Field.ProjectFieldId);

      foreach (var keyValuePair in newFieldValue)
      {
        var field = fields[keyValuePair.Key];

        field.Field.RequestEditAccess(currentUserId, character, claim);

        field.Value = keyValuePair.Value;

        field.MarkUsed();

        UpdateSpecialGroupsIfRequired(character, field);
      }

      if (character != null)
      {
        character.JsonData = fields.Values.SerializeFieldsFor(FieldBoundTo.Character);
      }
      if (claim != null)
      {
        claim.JsonData = fields.Values.SerializeFieldsFor(FieldBoundTo.Claim);
      }
    }

    private static void UpdateSpecialGroupsIfRequired([CanBeNull] Character character, [NotNull] FieldWithValue field)
    {
      if (!field.Field.HasSpecialGroup() || character == null) return;

      var valuesToAdd = field.GetDropdownValues().ToList();
      var valuesToRemove = field.Field.DropdownValues.Except(valuesToAdd);

      character.Groups.RemoveFromLinkList(valuesToRemove.Select(v => v.CharacterGroup));
      character.Groups.AddLinkList(valuesToAdd.Select(v => v.CharacterGroup).WhereNotNull().ToList());
    }
  }
}