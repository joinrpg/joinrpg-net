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
              .ToDictionary(pair => pair.Field.ProjectFieldId, pair => pair.Value));
    }

    private static Dictionary<int, string> DeserializeFieldValues(this IFieldContainter containter)
    {
        return JsonConvert.DeserializeObject<Dictionary<int, string>>(containter.JsonData ?? "") ??
               new Dictionary<int, string>();
    }

    private static IReadOnlyCollection<FieldWithValue> GetFieldsForContainers(Project project, params Dictionary<int, string>?[] containers)
    {
        var fields = project.GetOrderedFields().Select(pf => new FieldWithValue(pf, value: null)).ToList();

        foreach (var characterFieldValue in fields)
        {
            foreach (var data in containers.WhereNotNull())
            {
                var value = data.GetValueOrDefault(characterFieldValue.Field.ProjectFieldId);
                if (value != null)
                {
                    try
                    {
                        characterFieldValue.Value = value;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Problem parsing field value for field = {characterFieldValue.Field.ProjectFieldId}, Value = {value}", e);
                    }

                }
            }
        }

        return fields.AsReadOnly();
    }

    public static IReadOnlyCollection<FieldWithValue> GetFields(this Character character, ProjectInfo projectInfo)
        => GetFieldsForContainers(character.Project, character.ApprovedClaim?.DeserializeFieldValues(), character.DeserializeFieldValues());

    public static IReadOnlyCollection<FieldWithValue> GetFields(this CharacterView character, ProjectInfo projectInfo, Project project)
    => GetFieldsForContainers(project, character.ApprovedClaim?.DeserializeFieldValues(), character.DeserializeFieldValues());

    public static IReadOnlyCollection<FieldWithValue> GetFields(this Claim claim, ProjectInfo projectInfo)
    {
        if (claim.IsApproved)
        {
            return claim.Character!.GetFields(projectInfo);
        }
        var publicFields = claim.Project.ProjectFields.Where(f => f.IsPublic).Select(x => x.ProjectFieldId).ToList();
        return GetFieldsForContainers(claim.Project, claim.Character?.DeserializeFieldValues().Where(kv => publicFields.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value),
            claim.DeserializeFieldValues());
    }

    public static FieldWithValue? GetSingleField(this Claim claim, ProjectInfo projectInfo, ProjectFieldIdentification id)
    {
        return claim.GetFields(projectInfo).SingleOrDefault(f => f.Field.ProjectFieldId == id.ProjectFieldId);
    }

    public static IReadOnlyCollection<FieldWithValue> GetFieldsForClaimSource(this IClaimSource claimSource, ProjectInfo projectInfo)
    {
        if (claimSource is Character character)
        {
            return character.GetFields(projectInfo);
        }
        else
        {
            return GetFieldsForContainers(claimSource.Project);
        }
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
