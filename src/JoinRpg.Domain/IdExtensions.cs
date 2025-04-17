using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.Domain;
public static class IdExtensions
{
    public static CharacterIdentification GetId(this Character character) => new CharacterIdentification(character.ProjectId, character.CharacterId);

    public static PlotElementIdentification GetId(this PlotElement id) => new PlotElementIdentification(id.ProjectId, id.PlotFolderId, id.PlotElementId);

    public static CharacterGroupIdentification GetId(this CharacterGroup group) => new CharacterGroupIdentification(group.ProjectId, group.CharacterGroupId);
}
