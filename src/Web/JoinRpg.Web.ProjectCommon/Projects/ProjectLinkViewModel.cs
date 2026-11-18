namespace JoinRpg.Web.ProjectCommon.Projects;

public interface IProjectLinkViewModel
{
    ProjectIdentification ProjectId { get; }
    string ProjectName { get; }
}

public record ProjectLinkViewModel(ProjectIdentification ProjectId, string ProjectName) : IProjectLinkViewModel;
