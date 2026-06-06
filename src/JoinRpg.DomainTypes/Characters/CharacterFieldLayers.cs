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
