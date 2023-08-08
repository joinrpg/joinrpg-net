using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.Characters;

public class CharacterGroupLinkViewModel : ILinkable
{
    public int CharacterGroupId { get; }
    public string Name { get; }
    public bool IsPublic { get; }
    public LinkType LinkType => LinkType.ResultCharacterGroup;
    public string Identification => CharacterGroupId.ToString();
    int? ILinkable.ProjectId => ProjectId;

    public int ProjectId { get; }
    public bool IsActive { get; }

    public bool IsRoot { get; }

    public CharacterGroupLinkViewModel(CharacterGroup group)
    {
        CharacterGroupId = group.CharacterGroupId;
        Name = group.CharacterGroupName;
        IsPublic = group.IsPublic;
        ProjectId = group.ProjectId;
        IsActive = group.IsActive;
        IsRoot = group.IsRoot;
    }
}
