namespace JoinRpg.Web.ProjectMasterTools.Apis;

public class ApisIndexViewModel
{
    public int ProjectId { get; }
    public int RootGroupId { get; }

    public ApisIndexViewModel(ProjectInfo project)
    {
        ProjectId = project.ProjectId.Value;
        RootGroupId = project.RootCharacterGroupId.CharacterGroupId;
    }
}
