using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
  public static class FieldSaveHelper
  {
    [MustUseReturnValue, NotNull]
    public static IReadOnlyCollection<int> SaveCharacterFieldsImpl(
      int currentUserId, 
      [CanBeNull] Character character,
      [CanBeNull] Claim claim,
      [NotNull] IDictionary<int, string> newFieldValue)
    {
      if (newFieldValue == null) throw new ArgumentNullException(nameof(newFieldValue));

      if (claim != null && character != null && character.ApprovedClaim != claim)
      {
        throw new ArgumentException("Do not pass character if claim is not approved");
      }

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

        var editAccess = field.HasEditAccess(project.HasMasterAccess(currentUserId),
          character?.HasPlayerAccess(currentUserId) ?? false, claim.HasPlayerAccesToClaim(currentUserId), character);
        if (!editAccess)
        {
          throw new NoAccessToProjectException(project, currentUserId);
        }

        var normalizedValue = NormalizeValueBeforeAssign(field, keyValuePair.Value);
        if (field.Value != normalizedValue)
        {
          field.Value = normalizedValue;

          field.MarkUsed();
        }
      }

      if (character != null)
      {
        character.JsonData = fields.Values
          .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();
      }
      if (claim != null)
      {
        claim.JsonData = fields.Values
          .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Claim || character == null).SerializeFields();
      }

      return fields.Values.GenerateSpecialGroupsList();
    }

    private static string NormalizeValueBeforeAssign(FieldWithValue field, string toAssign)
    {
      return field.Field.FieldType == ProjectFieldType.Checkbox ? (toAssign.StartsWith("on") ? "on" : "") : toAssign;
    }
  }
}