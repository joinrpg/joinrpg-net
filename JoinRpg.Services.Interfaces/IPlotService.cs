using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IPlotService
  {
    Task CreatePlotFolder(int projectId, string masterTitle, string todo);
  }
}
