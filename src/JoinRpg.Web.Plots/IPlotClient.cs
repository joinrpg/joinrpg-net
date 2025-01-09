namespace JoinRpg.Web.Plots;
public interface IPlotClient
{
    Task<PlotFolderDto[]> GetPlotFoldersList(ProjectIdentification projectId);
}
