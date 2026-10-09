namespace JoinRpg.Web.Plots;

public interface IPlotUriLocator
{
    /// <summary>Вводная в списке своего сюжета — с якорем, раскрывающим её панель.</summary>
    Uri GetElementInFolderUri(PlotElementIdentification elementId);

    Uri GetEditElementUri(PlotElementIdentification elementId);

    /// <summary>Создание новой вводной копированием этой.</summary>
    Uri GetCopyElementUri(PlotElementIdentification elementId);

    Uri GetVersionUri(PlotVersionIdentification versionId);

    Uri GetPrintVersionUri(PlotVersionIdentification versionId);

    /// <summary>Куда отправлять перестановку вводных в сюжете.</summary>
    Uri GetReorderElementsUri(ProjectIdentification projectId);
}

public static class PlotElementAnchor
{
    /// <summary>Id панели вводной в списке сюжета — на него ведёт <see cref="IPlotUriLocator.GetElementInFolderUri"/>.</summary>
    public static string For(PlotElementIdentification elementId) => $"panelPlotElement{elementId.PlotElementId}";
}
