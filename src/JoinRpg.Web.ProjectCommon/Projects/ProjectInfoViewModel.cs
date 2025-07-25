using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.ProjectCommon.Projects;
public record class ProjectInfoViewModel(
    ProjectIdentification ProjectId,
    string Name,
    UserLinkViewModel[] Masters,
    //DateTimeOffset LastUpdatedAt,
    MarkupString DescriptionHtml,
    KogdaIgraIdentification[] KogdaIgraLinkedIds)
{
}
