using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.Web.ProjectCommon.ElementMoving;

public interface IMoveableListItem
{
    IProjectEntityId Id { get; }
    IProjectEntityId ParentId { get; }
    string DisplayText { get; }
    string Subtext { get; }
}
