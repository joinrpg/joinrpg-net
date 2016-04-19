namespace JoinRpg.Web.Models
{
  public interface IRootGroupAware
  {
    int ProjectId { get; }
    int RootGroupId { get; }
  }
}
