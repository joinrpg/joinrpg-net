using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IPlotService
  {
    Task CreatePlotFolder(int projectId, string masterTitle, string todo);
    Task EditPlotFolder(int projectId, int plotFolderId, string plotFolderMasterTitle, string todoField);
  }
}
