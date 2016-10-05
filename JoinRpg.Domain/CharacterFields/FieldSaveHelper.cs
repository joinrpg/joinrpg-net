using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
  public static class FieldSaveHelper
  {
    private abstract class FieldSaveStrategyBase
    {
      public abstract void Save(Character character, Claim claim, Dictionary<int, FieldWithValue> fields);

      protected static void UpdateSpecialGroups(Character character, Dictionary<int, FieldWithValue> fields)
      {
        var ids = fields.Values.GenerateSpecialGroupsList();
        var groupsToKeep = character.Groups.Where(g => !g.IsSpecial).Select(g => g.CharacterGroupId);
        character.ParentCharacterGroupIds = groupsToKeep.Union(ids).ToArray();
      }
    }
    private class SaveToCharacterOnlyStrategy : FieldSaveStrategyBase
    {
      public override void Save(Character character, Claim claim, Dictionary<int, FieldWithValue> fields)
      {
          character.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();
        UpdateSpecialGroups(character, fields);
      }
    }

    private class SaveToClaimOnlyStrategy : FieldSaveStrategyBase
    {
      public override void Save(Character character, Claim claim, Dictionary<int, FieldWithValue> fields)
      {
        //TODO do not save fields that have values same as character's
          claim.JsonData = fields.Values.SerializeFields();
      }
    }

    private class SaveToCharacterAndClaimStrategy : FieldSaveStrategyBase
    {
      public override void Save(Character character, Claim claim, Dictionary<int, FieldWithValue> fields)
      {
        character.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();
        
          claim.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Claim).SerializeFields();

        UpdateSpecialGroups(character, fields);
      }
    }

    public static void SaveCharacterFields(
      int currentUserId,
      [NotNull] Claim claim,
      [NotNull] IDictionary<int, string> newFieldValue)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      SaveCharacterFieldsImpl(currentUserId, claim.Character, claim, newFieldValue);
    }

    public static void SaveCharacterFields(
      int currentUserId,
      [NotNull] Character character,
      [NotNull] IDictionary<int, string> newFieldValue)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      SaveCharacterFieldsImpl(currentUserId, character, character.ApprovedClaim, newFieldValue);
    }

    private static void SaveCharacterFieldsImpl(
      int currentUserId, 
      [CanBeNull] Character character,
      [CanBeNull] Claim claim,
      [NotNull] IDictionary<int, string> newFieldValue)
    {
      if (newFieldValue == null) throw new ArgumentNullException(nameof(newFieldValue));

      FieldSaveStrategyBase strategy;
      if (claim == null)
      {
        strategy = new SaveToCharacterOnlyStrategy();
      }
      else if (!claim.IsApproved)
      {
        strategy = new SaveToClaimOnlyStrategy();
      }
      else
      {
        strategy = new SaveToCharacterAndClaimStrategy();
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

      var hasMasterAccess = project.HasMasterAccess(currentUserId);
      var characterAccess = character?.HasPlayerAccess(currentUserId) ?? false;
      var hasPlayerAccesToClaim = claim?.HasPlayerAccesToClaim(currentUserId) ?? false;

      foreach (var keyValuePair in newFieldValue)
      {
        var field = fields[keyValuePair.Key];

        var editAccess = field.HasEditAccess(hasMasterAccess,
          characterAccess, hasPlayerAccesToClaim, character ?? claim.GetTarget());
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

      strategy.Save(character, claim, fields);
    }

    private static string NormalizeValueBeforeAssign(FieldWithValue field, string toAssign)
    {
      return field.Field.FieldType == ProjectFieldType.Checkbox ? (toAssign.StartsWith("on") ? "on" : "") : toAssign;
    }
  }
}