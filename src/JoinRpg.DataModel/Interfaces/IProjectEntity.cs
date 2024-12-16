using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel;

public interface IProjectEntity : IProjectEntityWithId
{

    Project Project { get; }
}
