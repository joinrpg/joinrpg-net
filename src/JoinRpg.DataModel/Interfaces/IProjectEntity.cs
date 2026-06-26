using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DataModel;

public interface IProjectEntity : IProjectEntityWithId
{

    Project Project { get; }
}
