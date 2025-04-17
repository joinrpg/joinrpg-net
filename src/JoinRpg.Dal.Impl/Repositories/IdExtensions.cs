using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Dal.Impl.Repositories;
public static class IdExtensions
{
    public static CharacterIdentification GetId(this Character character) => new CharacterIdentification(character.ProjectId, character.CharacterId);

    public static CharacterGroupIdentification GetId(this CharacterGroup group) => new CharacterGroupIdentification(group.ProjectId, group.CharacterGroupId);
}
