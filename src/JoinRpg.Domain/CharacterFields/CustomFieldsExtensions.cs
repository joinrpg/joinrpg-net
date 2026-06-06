using System.Collections.ObjectModel;
using System.Text.Json;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Helpers;

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
          JsonSerializer.Serialize(
            fieldWithValues
              .Where(v => v.HasEditableValue)
              .ToDictionary(pair => pair.Field.Id.ProjectFieldId, pair => pair.Value));
    }

    private static FieldLayerContainer DeserializeFieldValues(this IFieldContainter containter, ProjectInfo projectInfo)
    {
        return FieldLayerContainer.DeserializeFieldLayer(projectInfo, containter.JsonData);
    }

    private static ReadOnlyCollection<FieldWithValue> GetFieldsForContainers(ProjectInfo project, params FieldLayerContainer?[] containers)
    {
        var fields = project.SortedFields.Select(pf => new FieldWithValue(pf, value: null)).ToList();

        foreach (var characterFieldValue in fields)
        {
            foreach (var container in containers.WhereNotNull())
            {
                if (container.LayerData.TryGetValue(characterFieldValue.Field.Id, out var layerField))
                {
                    try
                    {
                        characterFieldValue.Value = layerField.Value;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Problem parsing field value for field = {characterFieldValue.Field.Id}, Value = {layerField.Value}", e);
                    }
                }
            }
        }

        return fields.AsReadOnly();
    }

    public static IReadOnlyCollection<FieldWithValue> GetFields(this Character character, ProjectInfo projectInfo)
        => GetFieldsForContainers(projectInfo,
            character.ApprovedClaim is { } claim ? claim.DeserializeFieldValues(projectInfo) : null,
            character.DeserializeFieldValues(projectInfo));

    public static Dictionary<ProjectFieldIdentification, FieldWithValue> GetFieldsDict(this Character character, ProjectInfo projectInfo)
        => character.GetFields(projectInfo).ToDictionary(f => f.Field.Id);

    public static IReadOnlyCollection<FieldWithValue> GetFields(this CharacterView character, ProjectInfo projectInfo)
    => GetFieldsForContainers(projectInfo,
        character.ApprovedClaim is { } claim ? claim.DeserializeFieldValues(projectInfo) : null,
        character.DeserializeFieldValues(projectInfo));

    public static IReadOnlyCollection<FieldWithValue> GetFields(this Claim claim, ProjectInfo projectInfo)
    {
        if (claim.IsApproved)
        {
            return claim.Character!.GetFields(projectInfo);
        }
        return GetFieldsForContainers(projectInfo,
                claim.Character.DeserializeFieldValues(projectInfo).PublicOnly(),
            claim.DeserializeFieldValues(projectInfo));
    }

    public static FieldWithValue? GetSingleField(this Claim claim, ProjectInfo projectInfo, ProjectFieldIdentification id)
    {
        return claim.GetFields(projectInfo).SingleOrDefault(f => f.Field.Id == id);
    }

}
