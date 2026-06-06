using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters;

/// <summary>
/// Контейнер, связывающий метаданные проекта с послойными данными полей персонажа.
/// Слой — это срез значений полей, загруженный из внешнего источника (например, JSON).
/// </summary>
public class FieldLayerContainer(ProjectInfo projectInfo, IReadOnlyDictionary<int, string> layerData)
{
    public ProjectInfo ProjectInfo { get; } = projectInfo;
    public Dictionary<ProjectFieldIdentification, FieldWithValue> LayerData { get; } = CreateLayerData(projectInfo, layerData);

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
}
