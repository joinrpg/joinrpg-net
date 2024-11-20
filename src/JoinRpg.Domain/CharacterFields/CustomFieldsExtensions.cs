using System.Collections.ObjectModel;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using Newtonsoft.Json;

namespace JoinRpg.Domain;

public static class CustomFieldsExtensions
{
    public static string SerializeFields(this IEnumerable<FieldWithValue> fieldWithValues)
    {
        if (fieldWithValues == null)
        {
            throw new ArgumentNullException(nameof(fieldWithValues));
        }

        return
          JsonConvert.SerializeObject(
            fieldWithValues
              .Where(v => v.HasEditableValue)
              .ToDictionary(pair => pair.Field.Id.ProjectFieldId, pair => pair.Value));
    }

    private static Dictionary<int, string> DeserializeFieldValues(this IFieldContainter containter)
    {
        return JsonConvert.DeserializeObject<Dictionary<int, string>>(containter.JsonData ?? "") ?? [];
    }

    private static ReadOnlyCollection<FieldWithValue> GetFieldsForContainers(ProjectInfo project, params Dictionary<int, string>?[] containers)
    {
        var fields = project.SortedFields.Select(pf => new FieldWithValue(pf, value: null)).ToList();

        foreach (var characterFieldValue in fields)
        {
            foreach (var data in containers.WhereNotNull())
            {
                var value = data.GetValueOrDefault(characterFieldValue.Field.Id.ProjectFieldId);
                if (value != null)
                {
                    try
                    {
                        characterFieldValue.Value = value;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Problem parsing field value for field = {characterFieldValue.Field.Id}, Value = {value}", e);
                    }

                }
            }
        }

        return fields.AsReadOnly();
    }

    public static IReadOnlyCollection<FieldWithValue> GetFields(this Character character, ProjectInfo projectInfo)
        => GetFieldsForContainers(projectInfo, character.ApprovedClaim?.DeserializeFieldValues(), character.DeserializeFieldValues());

    public static Dictionary<ProjectFieldIdentification, FieldWithValue> GetFieldsDict(this Character character, ProjectInfo projectInfo)
        => character.GetFields(projectInfo).ToDictionary(f => f.Field.Id);

    public static IReadOnlyCollection<FieldWithValue> GetFields(this CharacterView character, ProjectInfo projectInfo)
    => GetFieldsForContainers(projectInfo, character.ApprovedClaim?.DeserializeFieldValues(), character.DeserializeFieldValues());

    public static IReadOnlyCollection<FieldWithValue> GetFields(this Claim claim, ProjectInfo projectInfo)
    {
        if (claim.IsApproved)
        {
            return claim.Character!.GetFields(projectInfo);
        }
        var publicFields = projectInfo.UnsortedFields.Where(f => f.IsPublic).Select(x => x.Id.ProjectFieldId).ToList();
        return GetFieldsForContainers(projectInfo, claim.Character.DeserializeFieldValues().Where(kv => publicFields.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value),
            claim.DeserializeFieldValues());
    }

    public static Dictionary<ProjectFieldIdentification, FieldWithValue> GetFieldsDict(this Claim claim, ProjectInfo projectInfo)
        => claim.GetFields(projectInfo).ToDictionary(f => f.Field.Id);

    public static FieldWithValue? GetSingleField(this Claim claim, ProjectInfo projectInfo, ProjectFieldIdentification id)
    {
        return claim.GetFields(projectInfo).SingleOrDefault(f => f.Field.Id == id);
    }

    public static AccessArguments GetAccessArguments(
      this IFieldContainter entityWithFields,
      int userId)
    {
        ArgumentNullException.ThrowIfNull(entityWithFields);

        if (entityWithFields is Claim claim)
        {
            return AccessArgumentsFactory.Create(claim, userId);
        }
        if (entityWithFields is Character character)
        {
            return AccessArgumentsFactory.Create(character, userId);
        }
        throw new NotSupportedException($"{entityWithFields.GetType()} is not supported to get fields for.");
    }
}
