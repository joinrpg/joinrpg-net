using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Interfaces;

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

    [Obsolete]
    public CharacterGroupLinkViewModel(CharacterGroup group)
    {
        CharacterGroupId = group.CharacterGroupId;
        Name = group.CharacterGroupName;
        IsPublic = group.IsPublic;
        ProjectId = group.ProjectId;
        IsActive = group.IsActive;
    }

    public CharacterGroupLinkViewModel(CharacterGroupInfo group)
    {
        CharacterGroupId = group.Id.CharacterGroupId;
        Name = group.Name;
        IsPublic = group.IsPublic;
        ProjectId = group.Id.ProjectId;
        IsActive = group.IsActive;
    }
}
