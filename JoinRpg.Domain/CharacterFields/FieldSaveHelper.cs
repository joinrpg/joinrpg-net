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
      protected Claim Claim { get; }
      protected Character Character { get; }
      private int CurrentUserId { get; }

      private Project Project { get; }

      protected FieldSaveStrategyBase(Claim claim, Character character, int currentUserId)
      {
        Claim = claim;
        Character = character;
        CurrentUserId = currentUserId;
        Project = character?.Project ?? claim?.Project;

        if (Project == null)
        {
          throw new ArgumentNullException("", "Either character or claim should be not null");
        }
      }

      

      public abstract void Save(Dictionary<int, FieldWithValue> fields);

      protected void UpdateSpecialGroups(Dictionary<int, FieldWithValue> fields)
      {
        var ids = fields.Values.GenerateSpecialGroupsList();
        var groupsToKeep = Character.Groups.Where(g => !g.IsSpecial).Select(g => g.CharacterGroupId);
        Character.ParentCharacterGroupIds = groupsToKeep.Union(ids).ToArray();
      }

      public Dictionary<int, FieldWithValue> LoadFields()
      {
        var fields =
          Project.GetFieldsWithoutOrder()
            .ToList()
            .FillIfEnabled(Claim, Character)
            .ToDictionary(f => f.Field.ProjectFieldId);
        return fields;
      }

      public void EnsureEditAccess(FieldWithValue field)
      {
        var hasMasterAccess = Project.HasMasterAccess(CurrentUserId);
        var characterAccess = Character?.HasPlayerAccess(CurrentUserId) ?? false;
        var hasPlayerAccesToClaim = Claim?.HasPlayerAccesToClaim(CurrentUserId) ?? false;
        var editAccess = field.HasEditAccess(hasMasterAccess,
          characterAccess, hasPlayerAccesToClaim, Character ?? Claim.GetTarget());
        if (!editAccess)
        {
          throw new NoAccessToProjectException(Project, CurrentUserId);
        }
      }
    }
    private class SaveToCharacterOnlyStrategy : FieldSaveStrategyBase
    {

      public SaveToCharacterOnlyStrategy(Claim claim, Character character, int currentUserId) : base(claim, character, currentUserId)
      {
      }

      public override void Save(Dictionary<int, FieldWithValue> fields)
      {
          Character.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();
        UpdateSpecialGroups(fields);
      }
    }

    private class SaveToClaimOnlyStrategy : FieldSaveStrategyBase
    {
      public SaveToClaimOnlyStrategy(Claim claim, Character character, int currentUserId) : base(claim, character, currentUserId)
      {
      }

      public override void Save(Dictionary<int, FieldWithValue> fields)
      {
        //TODO do not save fields that have values same as character's
          Claim.JsonData = fields.Values.SerializeFields();
      }
    }

    private class SaveToCharacterAndClaimStrategy : FieldSaveStrategyBase
    {
      public SaveToCharacterAndClaimStrategy(Claim claim, Character character, int currentUserId) : base(claim, character, currentUserId)
      {
      }

      public override void Save(Dictionary<int, FieldWithValue> fields)
      {
        Character.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();
        
          Claim.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Claim).SerializeFields();

        UpdateSpecialGroups(fields);
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
        strategy = new SaveToCharacterOnlyStrategy(null, character, currentUserId);
      }
      else if (!claim.IsApproved)
      {
        strategy = new SaveToClaimOnlyStrategy(claim, null, currentUserId);
      }
      else
      {
        strategy = new SaveToCharacterAndClaimStrategy(claim, character, currentUserId);
      }

      var fields = strategy.LoadFields();

     

      foreach (var keyValuePair in newFieldValue)
      {
        var field = fields[keyValuePair.Key];

        strategy.EnsureEditAccess(field);

        var normalizedValue = NormalizeValueBeforeAssign(field, keyValuePair.Value);
        if (field.Value != normalizedValue)
        {
          field.Value = normalizedValue;

          field.MarkUsed();
        }
      }

      strategy.Save(fields);
    }

    private static string NormalizeValueBeforeAssign(FieldWithValue field, string toAssign)
    {
      return field.Field.FieldType == ProjectFieldType.Checkbox
        ? (toAssign.StartsWith(FieldWithValue.CheckboxValueOn) ? FieldWithValue.CheckboxValueOn : "")
        : toAssign;
    }
  }
}