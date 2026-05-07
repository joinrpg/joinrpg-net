using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Plots;

public record class PlotAccessArguments(Permission[] Permissions, bool Published, ProjectLifecycleStatus ProjectLifecycleStatus)
{
    public bool HasEditAccess => Permissions.Contains(Permission.None) && ProjectLifecycleStatus != ProjectLifecycleStatus.Archived;

    public bool HasMasterAccess => Permissions.Contains(Permission.None);
    public bool HasPlotEditorAccess => Permissions.Contains(Permission.CanManagePlots) && ProjectLifecycleStatus != ProjectLifecycleStatus.Archived;

    public bool HasViewAccess => Permissions.Contains(Permission.None) || Published;
}
