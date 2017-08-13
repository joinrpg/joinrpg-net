using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public static class CustomFieldsExtensions
  {
    [NotNull, ItemNotNull, MustUseReturnValue]
    public static IReadOnlyList<FieldWithValue> GetFieldsNotFilled([NotNull] this Project project)
    {
      if (project == null) throw new ArgumentNullException(nameof(project));
      return
        project.GetOrderedFields()
          .Select(pf => new FieldWithValue(pf, null)).ToList().AsReadOnly();
    }

    /// <summary>
    /// That method is faster than GetFieldsNotFilled()
    /// </summary>
    public static IEnumerable<FieldWithValue> GetFieldsNotFilledWithoutOrder([NotNull] this Project project)
    {
      if (project == null) throw new ArgumentNullException(nameof(project));
      return project.ProjectFields.Select(pf => new FieldWithValue(pf, null));
    }

    [MustUseReturnValue]
    public static string SerializeFields([NotNull] this IEnumerable<FieldWithValue> fieldWithValues)
    {
      if (fieldWithValues == null) throw new ArgumentNullException(nameof(fieldWithValues));

      return
        JsonConvert.SerializeObject(
          fieldWithValues
            .Where(v => v.HasEditableValue)
            .ToDictionary(pair => pair.Field.ProjectFieldId, pair => pair.Value));
    }

    private static Dictionary<int, string> DeserializeFieldValues([CanBeNull] this IFieldContainter containter)
    {
      return JsonConvert.DeserializeObject<Dictionary<int, string>>(containter?.JsonData ?? "") ??
             new Dictionary<int, string>();
    }

    public static void MarkUsed([NotNull] this FieldWithValue field)
    {
      if (field == null) throw new ArgumentNullException(nameof(field));

      if (!field.Field.WasEverUsed)
      {
        field.Field.WasEverUsed = true;
      }

      if (field.Field.HasValueList())
      {
        foreach (var val in field.GetDropdownValues().Where(v => !v.WasEverUsed))
        {
          val.WasEverUsed = true;
        }
      }
    }

    public static void FillFrom([NotNull] this IReadOnlyCollection<FieldWithValue> characterFieldValues,
      [CanBeNull] IFieldContainter container)
    {
      if (characterFieldValues == null) throw new ArgumentNullException(nameof(characterFieldValues));
      if (container == null)
      {
        return;
      }
      var data = container.DeserializeFieldValues();
      foreach (var characterFieldValue in characterFieldValues)
      {
        var value = data.GetValueOrDefault(characterFieldValue.Field.ProjectFieldId);
        if (value != null)
        {
          characterFieldValue.Value = value;
        }
      }
    }

    public static IReadOnlyCollection<FieldWithValue> FillIfEnabled(
      [NotNull] this IReadOnlyCollection<FieldWithValue> characterFieldValues, [CanBeNull] Claim claim,
      [CanBeNull] Character character)
    {
      if (characterFieldValues == null) throw new ArgumentNullException(nameof(characterFieldValues));
      characterFieldValues.FillFrom(claim);
      characterFieldValues.FillFrom(character);
      return characterFieldValues;
    }

    public static IReadOnlyCollection<FieldWithValue> GetFields([NotNull] this Character character)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      return character.Project
        .GetFieldsNotFilled()
        .ToList()
        .FillIfEnabled(character.ApprovedClaim, character);
    }

    public static IReadOnlyCollection<FieldWithValue> GetFields([NotNull] this Claim claim)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      return claim.Project
        .GetFieldsNotFilled()
        .ToList()
        .FillIfEnabled(claim, claim.Character);
    }

    [MustUseReturnValue]
    public static Predicate<FieldWithValue> GetShowForUserPredicate(
      [NotNull] IFieldContainter entityWithFields,
      int userId)
    {
      if (entityWithFields == null) throw new ArgumentNullException(nameof(entityWithFields));

      var claim = entityWithFields as Claim;
      var character = entityWithFields as Character;

      if (claim != null)
      {
        return f => f.HasViewAccess(new AccessArguments(claim, userId));
      }
      if (character != null)
      {
        return f => f.HasViewAccess(new AccessArguments(character, userId));
      }
      throw new NotSupportedException($"{entityWithFields.GetType()} is not supported to get fields for.");
    }
  }
}