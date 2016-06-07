using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl
{
  internal static class FieldSaveHelper
  {
    [MustUseReturnValue, NotNull]
    public static IReadOnlyCollection<int> SaveCharacterFieldsImpl(int currentUserId, Character character, Claim claim,
      IDictionary<int, string> newFieldValue)
    {
      var project = character?.Project ?? claim?.Project;
      var fields =
        project.GetFields()
          .ToList()
          .FillIfEnabled(claim, character, currentUserId)
          .ToDictionary(f => f.Field.ProjectFieldId);

      foreach (var keyValuePair in newFieldValue)
      {
        var field = fields[keyValuePair.Key];

        field.Field.RequestEditAccess(currentUserId, character, claim);

        field.Value = keyValuePair.Value;

        field.MarkUsed();
      }

      if (character != null)
      {
        character.JsonData = fields.Values.SerializeFieldsFor(FieldBoundTo.Character);
      }
      if (claim != null)
      {
        claim.JsonData = fields.Values.SerializeFieldsFor(FieldBoundTo.Claim);
      }

      return
        fields.Values.Where(v => v.Field.HasSpecialGroup())
          .SelectMany(v => v.GetDropdownValues().Select(c => c.CharacterGroup.CharacterGroupId)).ToArray();
    }
  }
}