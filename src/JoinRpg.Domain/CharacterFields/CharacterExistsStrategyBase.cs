using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.CharacterFields;

internal abstract class CharacterExistsStrategyBase : FieldSaveStrategyBase
{
    protected new Character Character => base.Character!; //Character should always exists

    protected CharacterExistsStrategyBase(Claim? claim, Character character, int currentUserId, IFieldDefaultValueGenerator generator, ProjectInfo projectInfo)
        : base(claim, character, currentUserId, generator, projectInfo)
    {
    }

    protected void UpdateSpecialGroups(Dictionary<int, FieldWithValue> fields)
    {
        var ids = fields.Values.SelectMany(v => v.GetSpecialGroupsToApply()).ToArray();
        var groupsToKeep = Character.Groups.Where(g => !g.IsSpecial)
            .Select(g => g.CharacterGroupId);
        Character.ParentCharacterGroupIds = groupsToKeep.Union(ids).ToArray();
    }

    protected void SetCharacterDescription(Dictionary<int, FieldWithValue> fields)
    {
        if (Project.Details.CharacterDescription != null)
        {
            Character.Description = new MarkdownString(
                fields[Project.Details.CharacterDescription.ProjectFieldId].Value);
        }


        if (Project.Details.CharacterNameField == null)
        {
            SetCharacterNameFromPlayer();
        }
        else
        {
            var name = fields[Project.Details.CharacterNameField.ProjectFieldId].Value;

            Character.CharacterName = string.IsNullOrWhiteSpace(name) ?
                Character.CharacterName = "CHAR" + Character.CharacterId
                : name;
        }

    }

    public override void Save(Dictionary<int, FieldWithValue> fields)
    {
        base.Save(fields);

        SetCharacterDescription(fields);

        UpdateSpecialGroups(fields);
    }
}
