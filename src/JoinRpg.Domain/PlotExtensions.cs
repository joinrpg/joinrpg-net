namespace JoinRpg.Domain;

public static class PlotExtensions
{
    public static PlotElementTexts LastVersion(this PlotElement e) => e.Texts.OrderByDescending(text => text.Version).First();
}
