using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters;

public record class CharacterFieldLayers(FieldLayerContainer? ClaimLayer, FieldLayerContainer CharacterLayer, AccessArguments AccessArguments)
{
    public FieldWithValue? GetFieldValue(ProjectFieldIdentification projectFieldId)
    {
        var field = CharacterLayer.ProjectInfo.GetFieldById(projectFieldId);

        return field.BoundTo switch
        {
            FieldBoundTo.Claim => ClaimLayer?.GetFromLayer(projectFieldId, AccessArguments),
            FieldBoundTo.Character => ClaimLayer?.GetFromLayer(projectFieldId, AccessArguments) ?? CharacterLayer.GetFromLayer(projectFieldId, AccessArguments),
            _ => throw new InvalidOperationException(),
        };
    }

    /// <summary>
    /// Полный набор полей для сохранения: значение каждого поля, разрешённое по слоям
    /// (claim перекрывает character для character-bound), без фильтрации по доступу на просмотр.
    /// В отличие от <see cref="GetSortedFieldsForView"/> возвращает запись для КАЖДОГО поля (в т.ч. пустую).
    /// </summary>
    public IReadOnlyCollection<FieldWithValue> GetAllFieldsForEdit()
    {
        var result = new List<FieldWithValue>();
        foreach (var field in CharacterLayer.ProjectInfo.SortedFields)
        {
            var resolved = field.BoundTo switch
            {
                FieldBoundTo.Claim => ClaimLayer?.LayerData.GetValueOrDefault(field.Id),
                FieldBoundTo.Character =>
                    ClaimLayer?.LayerData.GetValueOrDefault(field.Id)
                    ?? CharacterLayer.LayerData.GetValueOrDefault(field.Id),
                _ => throw new InvalidOperationException(),
            };
            result.Add(resolved ?? new FieldWithValue(field, value: null));
        }
        return result;
    }

    public IEnumerable<FieldWithValue> GetSortedFieldsForView()
    {
        foreach (var field in CharacterLayer.ProjectInfo.SortedFields)
        {
            var value = GetFieldValue(field.Id);
            if (value?.HasViewableValue == true)
            {
                yield return value;
            }
        }
    }
}
