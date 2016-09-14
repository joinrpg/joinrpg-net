using System;
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
    [MustUseReturnValue, NotNull]
    public static IReadOnlyCollection<int> SaveCharacterFieldsImpl(int currentUserId, [CanBeNull] Character character, [CanBeNull] Claim claim,
      [NotNull] IDictionary<int, string> newFieldValue)
    {
      if (newFieldValue == null) throw new ArgumentNullException(nameof(newFieldValue));

      var project = character?.Project ?? claim?.Project;

      if (project == null)
      {
        throw new ArgumentNullException("", "Either character or claim should be not null");
      }

      var fields =
        project.GetFieldsWithoutOrder()
          .ToList()
          .FillIfEnabled(claim, character, currentUserId)
          .ToDictionary(f => f.Field.ProjectFieldId);

      foreach (var keyValuePair in newFieldValue)
      {
        var field = fields[keyValuePair.Key];

        field.Field.RequestEditAccess(currentUserId, character, claim);

        if (field.Field.FieldType == ProjectFieldType.Checkbox)
        {
          field.Value = keyValuePair.Value.StartsWith("on") ? "on" : "";
        }
        else
        {
          field.Value = keyValuePair.Value;
        }

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

      return fields.Values.GenerateSpecialGroupsList();
    }
  }
}