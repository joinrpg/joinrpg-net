using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters;

public record class CharacterFieldLayers(FieldLayerContainer? ClaimLayer, FieldLayerContainer CharacterLayer, AccessArguments AccessArguments)
{
    /// <summary>
    /// Слои глазами ещё не утверждённой заявки: из персонажа видны только публичные поля.
    /// </summary>
    /// <remarks>
    /// Пока заявка не утверждена, её игрок не имеет доступа к персонажу. Непубличные значения
    /// персонажа не должны ни показываться в такой заявке, ни участвовать в сохранении её полей:
    /// иначе они попадут в слой заявки и при переносе заявки на другого персонажа с последующим
    /// принятием перезапишут его поля.
    ///
    /// Правило живёт здесь одно на всех: <see cref="CharacterInfo"/>.GetFieldLayers слой персонажа
    /// намеренно не фильтрует, потому что поверх него считаются взносы, где фильтрация была бы
    /// неверна. Значит каждый, кто строит слои для неутверждённой заявки, обязан звать это.
    /// </remarks>
    public static CharacterFieldLayers ForUnapprovedClaim(
        FieldLayerContainer claimLayer,
        FieldLayerContainer characterLayer,
        AccessArguments accessArguments)
        => new(claimLayer, characterLayer.PublicOnly(), accessArguments);

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
    /// <remarks>
    /// Отдаются КОПИИ значений, а не объекты самих слоёв: <see cref="FieldWithValue"/> изменяем, и
    /// вызывающие его правят (стратегии сохранения присваивают новые значения, вьюмодель формы
    /// заявки — подставляет значения из POST). Если бы отдавались объекты слоя, присваивание
    /// меняло бы «значение до операции» прямо в слое. Так и было: публичное character-bound поле
    /// попадало в слой персонажа, стратегия <c>SaveToClaimOnlyStrategy</c> сравнивала новое
    /// значение с ним же — считала, что игрок ничего не менял, и значение из заявки не
    /// сохранялось (показывалось значение из шаблона).
    /// </remarks>
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
            result.Add(new FieldWithValue(field, resolved?.Value));
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
