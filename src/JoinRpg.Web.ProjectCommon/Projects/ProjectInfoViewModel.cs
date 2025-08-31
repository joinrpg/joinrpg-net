using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
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

public record class KogdaIgraCardViewModel(
    Uri KogdaIgraUri,
    string Name,
    DateOnly Begin,
    DateOnly End,
    string RegionName,
    string MasterGroupName,
    Uri? SiteUri);

public class ProjectDetailsViewModel(ProjectInfo project, MarkupString projectDescription, IReadOnlyCollection<ClaimWithPlayer> claims, IReadOnlyCollection<KogdaIgraCardViewModel> kogdaIgras)
{

    public ProjectLifecycleStatus Status { get; set; } = project.ProjectStatus;
    public ProjectIdentification ProjectId { get; } = project.ProjectId;

    [Display(Name = "Дата создания")]
    public DateOnly CreatedDate { get; } = project.CreateDate;
    public IEnumerable<UserLinkViewModel> Masters { get; } = project.Masters.Select(acl => acl.ToUserLinkViewModel());

    [DisplayName("Анонс проекта")]
    public MarkupString ProjectAnnounce { get; } = projectDescription;

    public bool HasMyClaims { get; } = claims.Count > 0;

    [DisplayName("Название проекта")]
    public string ProjectName { get; } = project.ProjectName;

    public string Title => "Игра «" + ProjectName + "»";

    public IReadOnlyCollection<KogdaIgraCardViewModel> KogdaIgras { get; set; } = kogdaIgras;
}
