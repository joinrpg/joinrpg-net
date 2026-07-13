using System.Diagnostics.CodeAnalysis;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal abstract class CharacterExistsStrategyBase(Claim? claim, Character character, UserIdentification currentUserId, IFieldDefaultValueGenerator generator, ProjectInfo projectInfo)
    : FieldSaveStrategyBase(claim, character, currentUserId, generator, projectInfo, AccessArgumentsFactory.Create(character, currentUserId, projectInfo))
{
    protected new Character Character => base.Character!; //Character should always exists

    protected void UpdateSpecialGroups(Dictionary<int, FieldWithValue> fields)
    {
        var specialGroupIds = fields.Values.SelectMany(v => v.GetSpecialGroupsToApply());
        var regularGroupIds = Character.GetDirectGroups(ProjectInfo).Where(g => !g.IsSpecial).Select(g => g.Id);

        Character.ParentCharacterGroupIds = [.. regularGroupIds.Union(specialGroupIds).Select(x => x.Id)];
    }

    protected void SetCharacterDescription(Dictionary<int, FieldWithValue> fields)
    {
        if (ProjectInfo.CharacterDescriptionField is ProjectFieldInfo descField)
        {
            Character.Description = new MarkdownDbValue(
                GetFieldValue(descField));
        }


        if (ProjectInfo.CharacterNameField is not ProjectFieldInfo nameField)
        {
            SetCharacterNameFromPlayer();
        }
        else
        {
            var name = GetFieldValue(nameField);

            Character.CharacterName = string.IsNullOrWhiteSpace(name) ?
                "CHAR" + Character.CharacterId
                : name;
        }

        string? GetFieldValue(ProjectFieldInfo field) => fields[field.Id.ProjectFieldId].Value;
    }

    public override void Save(Dictionary<int, FieldWithValue> fields)
    {
        base.Save(fields);

        SetCharacterDescription(fields);

        UpdateSpecialGroups(fields);
    }

    [DoesNotReturn]
    protected override void ThrowRequiredField(FieldWithValue field) => throw new CharacterFieldRequiredException(field.Field.Name, field.Field.Id, new(ProjectInfo.ProjectId, Character.CharacterId));
}
