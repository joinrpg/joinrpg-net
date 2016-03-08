using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public static class CustomFieldsExtensions
  {
    public static IEnumerable<FieldWithValue> GetFields(this Project project)
    {
      return
        project.GetOrderedFields()
          .Select(pf => new FieldWithValue(pf, null));
    }

    [MustUseReturnValue]
    public static string SerializeFieldsFor([NotNull] this IEnumerable<FieldWithValue> values, FieldBoundTo fieldBoundTo)
    {
      if (values == null) throw new ArgumentNullException(nameof(values));
      return
        JsonConvert.SerializeObject(
          values.Where(v => v.Field.FieldBoundTo == fieldBoundTo)
            .ToDictionary(pair => pair.Field.ProjectFieldId, pair => pair.Value));
    }

    private static Dictionary<int, string> DeserializeFieldValues([CanBeNull] this IFieldContainter containter)
    {
      return JsonConvert.DeserializeObject<Dictionary<int, string>>(containter?.JsonData ?? "") ?? new Dictionary<int, string>();
    }

    public static void MarkUsed([NotNull] this FieldWithValue field)
    {
      if (field == null) throw new ArgumentNullException(nameof(field));
      if (!field.Field.WasEverUsed)
      {
        field.Field.WasEverUsed = true;
      }

      foreach (var val in field.GetDropdownValues().Where(v => !v.WasEverUsed))
      {
        val.WasEverUsed = true;
      }
    }

    public static void FillFrom([NotNull] this ICollection<FieldWithValue> characterFieldValues, IFieldContainter container)
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

    public static ICollection<FieldWithValue> FillIfEnabled(this ICollection<FieldWithValue> characterFieldValues, Claim claim, Character character, int? currentUserId)
    {
      if (claim != null && (claim.HasMasterAccess(currentUserId) || claim.HasPlayerAccesToClaim(currentUserId)))
      {
        characterFieldValues.FillFrom(claim);
      }
      if (character != null && (character.HasMasterAccess(currentUserId) || character.HasPlayerAccess(currentUserId)))
      {
        characterFieldValues.FillFrom(character);
      }
      return characterFieldValues;
    }
  }
}