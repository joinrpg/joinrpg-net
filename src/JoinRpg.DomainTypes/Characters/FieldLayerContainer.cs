using System.Text.Json;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters;

/// <summary>
/// Контейнер, связывающий метаданные проекта с послойными данными полей персонажа.
/// Слой — это срез значений полей, загруженный из внешнего источника (например, JSON).
/// </summary>
public class FieldLayerContainer
{
    public ProjectInfo ProjectInfo { get; }
    public IReadOnlyDictionary<ProjectFieldIdentification, FieldWithValue> LayerData { get; }

    public FieldLayerContainer(ProjectInfo projectInfo, IReadOnlyDictionary<int, string> layerData) : this(projectInfo, CreateLayerData(projectInfo, layerData))
    {
    }

    public FieldLayerContainer(ProjectInfo projectInfo, IReadOnlyDictionary<ProjectFieldIdentification, FieldWithValue> layerData)
    {
        ProjectInfo = projectInfo;
        LayerData = layerData;
    }

    private static Dictionary<ProjectFieldIdentification, FieldWithValue> CreateLayerData(ProjectInfo projectInfo, IReadOnlyDictionary<int, string> layerData)
    {
        var result = new Dictionary<ProjectFieldIdentification, FieldWithValue>(layerData.Count);

        foreach (var (fieldId, value) in layerData)
        {
            var field = projectInfo.GetFieldById(new ProjectFieldIdentification(projectInfo.ProjectId, fieldId));
            result.Add(field.Id, new FieldWithValue(field, value));
        }

        return result;
    }

    /// <summary>
    ///   Возвращает копию контейнера только с публичными полями.
    /// </summary>
    public FieldLayerContainer PublicOnly()
    {
        var filtered = LayerData
            .Where(kvp => kvp.Value.Field.IsPublic)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (filtered.Count == LayerData.Count)
        {
            return this;
        }

        return new FieldLayerContainer(ProjectInfo, filtered);
    }

    public static FieldLayerContainer DeserializeFieldLayer(ProjectInfo projectInfo, string jsonData)
    {
        // System.Text.Json бросает на пустой/null строке, поэтому отдаём пустой словарь явно
        // (Newtonsoft.Json на "" возвращал null -> []).
        var dict = string.IsNullOrEmpty(jsonData)
            ? []
            : JsonSerializer.Deserialize<Dictionary<int, string>>(jsonData) ?? [];
        return new FieldLayerContainer(projectInfo, dict);
    }
}
