using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IPlotService
  {
    Task CreatePlotFolder(int projectId, string masterTitle, string todo);
    Task EditPlotFolder(int projectId, int plotFolderId, string plotFolderMasterTitle, string todoField);
    Task AddPlotElement(int projectId, int plotFolderId, string content, string todoField, IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars);
    Task DeleteFolder(int projectId, int plotFolderId, int currentUserId);
    Task DeleteElement(int projectId, int plotFolderId, int plotelementid, int currentUserId);
    Task EditPlotElement(int projectId, int plotFolderId, int plotelementid, string contents, string todoField, IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars, bool isCompleted, int currentUserId);
    Task MoveElement(int currentUserId, int projectId, int plotElementId, int parentCharacterId, int direction);
  }
}
