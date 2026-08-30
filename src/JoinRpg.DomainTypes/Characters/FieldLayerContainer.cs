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

    /// <summary>
    /// Слой из «сырых» значений полей: из JSON, из формы или из API. Значение нулябельно —
    /// <c>null</c> означает «поле не заполнено».
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// В наборе есть поле, которого нет в проекте.
    /// </exception>
    public FieldLayerContainer(ProjectInfo projectInfo, IReadOnlyDictionary<int, string?> layerData)
        : this(projectInfo, CreateLayerData(projectInfo, layerData))
    {
    }

    /// <summary>
    /// Значение поля в этом слое. <c>null</c>, если поля в слое нет или оно не заполнено.
    /// </summary>
    public string? GetValue(ProjectFieldInfo field) => LayerData.GetValueOrDefault(field.Id)?.Value;

    /// <summary>Пустой слой — «полей нет» / «ничего не меняем».</summary>
    public static FieldLayerContainer Empty(ProjectInfo projectInfo)
        => new(projectInfo, new Dictionary<ProjectFieldIdentification, FieldWithValue>());

    public FieldLayerContainer(ProjectInfo projectInfo, IReadOnlyDictionary<ProjectFieldIdentification, FieldWithValue> layerData)
    {
        ProjectInfo = projectInfo;
        LayerData = layerData;
    }

    private static Dictionary<ProjectFieldIdentification, FieldWithValue> CreateLayerData(ProjectInfo projectInfo, IReadOnlyDictionary<int, string?> layerData)
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
    /// Копия слоя без полей, которые такому зрителю не видны.
    /// </summary>
    /// <remarks>
    /// Обобщение <see cref="PublicOnly"/>: для игрока без доступа к персонажу даёт ровно тот же
    /// набор (непубличные character-bound поля отсекаются по <c>AnyAccessToCharacter</c>), но
    /// правило записано один раз — через <see cref="ProjectFieldInfo.HasViewAccess"/>.
    ///
    /// Фильтровать слой нужно осознанно и на месте: <see cref="CharacterInfo.GetFieldLayers"/>
    /// отдаёт слой персонажа как есть, потому что поверх него считаются в том числе взносы
    /// (см. <c>FinanceExtensions</c>), а там фильтрация по правам всё сломала бы.
    /// </remarks>
    public FieldLayerContainer VisibleFor(AccessArguments accessArguments)
    {
        var filtered = LayerData
            .Where(kvp => kvp.Value.Field.HasViewAccess(accessArguments))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (filtered.Count == LayerData.Count)
        {
            return this;
        }

        return new FieldLayerContainer(ProjectInfo, filtered);
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

    public static FieldLayerContainer DeserializeFieldLayer(ProjectInfo projectInfo, string? jsonData)
    {
        // System.Text.Json бросает на пустой/null строке, поэтому отдаём пустой словарь явно
        // (Newtonsoft.Json на "" возвращал null -> []).
        var dict = string.IsNullOrEmpty(jsonData)
            ? []
            : JsonSerializer.Deserialize<Dictionary<int, string?>>(jsonData) ?? [];
        return new FieldLayerContainer(projectInfo, dict);
    }

    public FieldWithValue? GetFromLayer(ProjectFieldIdentification fieldId, AccessArguments accessArguments)
    {
        var field = ProjectInfo.GetFieldById(fieldId);
        if (!field.HasViewAccess(accessArguments))
        {
            return null;
        }

        return LayerData.GetValueOrDefault(fieldId) ?? (!field.CanHaveValue ? new FieldWithValue(field, null) : null);
    }
}
