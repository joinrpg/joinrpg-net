namespace JoinRpg.DataModel
{
  public interface IProjectEntity
  {
    Project Project { get; }
    int ProjectId { get; }

    int Id { get; }
  }
}