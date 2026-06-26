using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.Interfaces;

public interface IProjectEntityWithId : IOrderableEntity
{

    int ProjectId { get; }

    ProjectIdentification ProjectIdentification => new ProjectIdentification(ProjectId);
}
