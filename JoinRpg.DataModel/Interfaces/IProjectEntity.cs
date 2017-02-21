using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  public interface IProjectEntity : IOrderableEntity
  {
    Project Project { get; }
    int ProjectId { get; }
  }
}