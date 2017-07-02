using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
  //TODO That should be service with interface and costructed via DI container
  public static class FieldSaveHelper
  {
    private abstract class FieldSaveStrategyBase
    {
      protected Claim Claim { get; }
      protected Character Character { get; }
      private int CurrentUserId { get; }
      public IFieldDefaultValueGenerator Generator { get; }
      private Project Project { get; }
      private List<FieldWithValue> UpdatedFields { get; } = new List<FieldWithValue>();

      protected FieldSaveStrategyBase(Claim claim, Character character, int currentUserId, IFieldDefaultValueGenerator generator)
      {
        Claim = claim;
        Character = character;
        CurrentUserId = currentUserId;
        Generator = generator;
        Project = character?.Project ?? claim?.Project;

        if (Project == null)
        {
          throw new ArgumentNullException("", "Either character or claim should be not null");
        }
      }

      public IReadOnlyCollection<FieldWithValue> GetUpdatedFields() => UpdatedFields;

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

      public void AssignFieldValue(FieldWithValue field, string newValue)
      {
        if (field.Value == newValue) return;

        field.Value = newValue;
        field.MarkUsed();
        UpdatedFields.Add(field);
      }

      public string GenerateDefaultValue(FieldWithValue field)
      {
        string newValue;
        switch (field.Field.FieldBoundTo)
        {
          case FieldBoundTo.Character:
            newValue = Generator.CreateDefaultValue(Character, field.Field);
            break;
          case FieldBoundTo.Claim:
            newValue = Generator.CreateDefaultValue(Claim, field.Field);
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
        return newValue;
      }
    }
    private class SaveToCharacterOnlyStrategy : FieldSaveStrategyBase
    {

      public SaveToCharacterOnlyStrategy(Claim claim, Character character, int currentUserId, IFieldDefaultValueGenerator generator) : base(claim, character, currentUserId, generator)
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
      public SaveToClaimOnlyStrategy(Claim claim, Character character, int currentUserId, IFieldDefaultValueGenerator generator) : base(claim, character, currentUserId, generator)
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
      public SaveToCharacterAndClaimStrategy(Claim claim, Character character, int currentUserId, IFieldDefaultValueGenerator generator) : base(claim, character, currentUserId, generator)
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

    [MustUseReturnValue]
    public static IReadOnlyCollection<FieldWithValue> SaveCharacterFields(
      int currentUserId,
      [NotNull] Claim claim,
      [NotNull] IDictionary<int, string> newFieldValue,
      IFieldDefaultValueGenerator generator)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      return SaveCharacterFieldsImpl(currentUserId, claim.Character, claim, newFieldValue, generator);
    }

    [MustUseReturnValue]
    public static IReadOnlyCollection<FieldWithValue> SaveCharacterFields(
      int currentUserId,
      [NotNull] Character character,
      [NotNull] IDictionary<int, string> newFieldValue,
      IFieldDefaultValueGenerator generator)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      return SaveCharacterFieldsImpl(currentUserId, character, character.ApprovedClaim, newFieldValue, generator);
    }

    [MustUseReturnValue]
    private static IReadOnlyCollection<FieldWithValue> SaveCharacterFieldsImpl(int currentUserId,
      [CanBeNull] Character character, [CanBeNull] Claim claim, [NotNull] IDictionary<int, string> newFieldValue,
      IFieldDefaultValueGenerator generator)
    {
      if (newFieldValue == null) throw new ArgumentNullException(nameof(newFieldValue));

      FieldSaveStrategyBase strategy;
      if (claim == null)
      {
        strategy = new SaveToCharacterOnlyStrategy(null, character, currentUserId, generator);
      }
      else if (!claim.IsApproved)
      {
        strategy = new SaveToClaimOnlyStrategy(claim, null, currentUserId, generator);
      }
      else
      {
        strategy = new SaveToCharacterAndClaimStrategy(claim, character, currentUserId, generator);
      }

      var fields = strategy.LoadFields();

      foreach (var keyValuePair in newFieldValue)
      {
        var field = fields[keyValuePair.Key];

        strategy.EnsureEditAccess(field);

        var normalizedValue = NormalizeValueBeforeAssign(field, keyValuePair.Value);
        strategy.AssignFieldValue(field, normalizedValue);
      }

      foreach (var field in fields.Values.Where(f => !f.HasEditableValue && f.Field.CanHaveValue()))
      {
        var newValue = strategy.GenerateDefaultValue(field);

        strategy.AssignFieldValue(field, newValue);
      }

      strategy.Save(fields);
      return strategy.GetUpdatedFields();
    }

    private static string NormalizeValueBeforeAssign(FieldWithValue field, string toAssign)
    {
      return field.Field.FieldType == ProjectFieldType.Checkbox
        ? (toAssign.StartsWith(FieldWithValue.CheckboxValueOn) ? FieldWithValue.CheckboxValueOn : "")
        : toAssign;
    }
  }
}