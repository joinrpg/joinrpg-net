namespace JoinRpg.DataModel
{
  public interface IProjectSubEntity
  {
    Project Project { get; set; }
    int ProjectId { get; set; }

    int Id { get; }
  }
}