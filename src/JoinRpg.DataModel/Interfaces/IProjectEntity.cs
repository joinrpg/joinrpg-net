using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

public interface IProjectEntity : IOrderableEntity
{
    [NotNull]
    Project Project { get; }
}
