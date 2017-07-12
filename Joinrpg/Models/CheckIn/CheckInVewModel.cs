using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.CheckIn
{
  public class CheckInIndexViewModel : IProjectIdAware
  {
    public CheckInIndexViewModel(Project project)
    {
      ProjectId = project.ProjectId;
    }

    public int ProjectId { get; }
  }
}