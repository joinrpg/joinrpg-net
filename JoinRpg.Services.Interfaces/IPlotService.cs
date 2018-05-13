using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IPlotService
    {
        Task CreatePlotFolder(int projectId, [NotNull] string masterTitle, [NotNull] string todo);
        Task EditPlotFolder(int projectId, int plotFolderId, string plotFolderMasterTitle, string todoField);

        Task CreatePlotElement(int projectId, int plotFolderId, string content, string todoField,
          IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars, PlotElementType elementType);

        Task DeleteFolder(int projectId, int plotFolderId);
        Task DeleteElement(int projectId, int plotFolderId, int plotelementid);

        Task EditPlotElement(int projectId, int plotFolderId, int plotelementid, string contents, string todoField,
          IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars);

        Task MoveElement(int projectId, int plotElementId, int parentCharacterId, int direction);

        Task PublishElementVersion(IPublishPlotElementModel model);

        Task EditPlotElementText(int projectId, int plotFolderId, int plotelementid, string content, string todoField);
    }
}
