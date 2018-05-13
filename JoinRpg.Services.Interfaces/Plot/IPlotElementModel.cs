namespace JoinRpg.Services.Interfaces
{
    public interface IPlotElementModel
    {

        int ProjectId { get; }

        int PlotFolderId { get; }

        int PlotElementId { get; }

        int? Version { get; }

    }
}
