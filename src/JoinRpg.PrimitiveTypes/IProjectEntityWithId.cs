using JoinRpg.Helpers;

namespace JoinRpg.PrimitiveTypes;
public interface IProjectEntityWithId : IOrderableEntity
{

    int ProjectId { get; }

    ProjectIdentification ProjectIdentification => new ProjectIdentification(ProjectId);
}
