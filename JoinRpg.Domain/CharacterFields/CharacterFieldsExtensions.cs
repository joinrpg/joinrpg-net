using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace JoinRpg.Domain
{
  public static class CharacterFieldsExtensions
  {
    public static IEnumerable<CharacterFieldValue> GetPresentFields([NotNull] this Character character)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      return character.GetAllFields().Where(val => val.IsPresent);
    }

    public static IEnumerable<CharacterFieldValue> GetAllFields([NotNull] this Character character)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      var deserialized = character.DeserializeFieldValues();
      return character.Project.GetOrderedFields()
        .Select(
          projectCharacterField => GetFieldFromData(deserialized, projectCharacterField));
    }
    public static string SerializeFields([NotNull, ItemNotNull] this IEnumerable<CharacterFieldValue> values)
    {
      if (values == null) throw new ArgumentNullException(nameof(values));
      return JsonConvert.SerializeObject(values.ToDictionary(pair => pair.Field.ProjectCharacterFieldId, pair => pair.Value));
    }

    private static CharacterFieldValue GetFieldFromData(IReadOnlyDictionary<int, string> data, ProjectCharacterField projectCharacterField)
    {
      return new CharacterFieldValue(projectCharacterField,
        data.ContainsKey(projectCharacterField.ProjectCharacterFieldId)
          ? data[projectCharacterField.ProjectCharacterFieldId]
          : null);
    }

    private static Dictionary<int, string> DeserializeFieldValues(this Character character)
    {
      return JsonConvert.DeserializeObject<Dictionary<int, string>>(character.JsonData ?? "") ??
             new Dictionary<int, string>();
    }
  }
}