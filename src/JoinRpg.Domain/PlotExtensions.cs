using System.Diagnostics.CodeAnalysis;
using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.Domain;

public static class PlotExtensions
{
    public static PlotElementTexts LastVersion(this PlotElement e) => e.Texts.OrderByDescending(text => text.Version).First();

    public static PlotElementTexts? SpecificVersion(this PlotElement e, int version) => e.Texts.SingleOrDefault(text => text.Version == version);


    public static TargetsInfo ToTarget(this PlotElement element)
    {
        return new TargetsInfo(
                            [.. element.TargetCharacters.Select(x => new CharacterTarget(x.GetId(), x.CharacterName))],
                            [.. element.TargetGroups.Select(x => new GroupTarget(x.GetId(), x.CharacterGroupName))]);
    }

    [return: NotNullIfNotNull(nameof(version))]
    public static PlotVersionIdentification? GetVersionId(this PlotElement element, int? version)
        => version is null ? null : new(element.ProjectId, element.PlotFolderId, element.PlotElementId, version.Value);
}
