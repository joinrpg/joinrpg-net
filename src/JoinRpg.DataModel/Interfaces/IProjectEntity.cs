using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel;

public interface IProjectEntity : IProjectEntityWithId
{

    Project Project { get; }
}

public interface IProjectEntityWithId : IOrderableEntity
{

    int ProjectId { get; }

    ProjectIdentification ProjectIdentification => new ProjectIdentification(ProjectId);
}
