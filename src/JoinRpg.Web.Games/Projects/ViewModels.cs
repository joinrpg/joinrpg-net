using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Claims;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Games.Projects;

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
    KogdaIgraIdentification KogdaIgraId,
    Uri KogdaIgraUri,
    string Name,
    DateOnly Begin,
    DateOnly End,
    string RegionName,
    string MasterGroupName, Uri? SiteUri);

[method: JsonConstructor]
public class ProjectDetailsViewModel(ProjectInfo project, MarkupString projectDescription, IReadOnlyCollection<ClaimLinkViewModel> claims,
    IReadOnlyCollection<KogdaIgraCardViewModel> kogdaIgras,
    bool disableKogdaIgraMapping,
    IReadOnlyCollection<CaptainAccessRule> captainAccessRules)
{
    public ProjectLifecycleStatus Status { get; set; } = project.ProjectStatus;
    public ProjectIdentification ProjectId { get; } = project.ProjectId;

    [Display(Name = "Дата создания")]
    public DateOnly CreatedDate { get; } = project.CreateDate;
    public IEnumerable<UserLinkViewModel> Masters { get; } = project.Masters.Select(acl => acl.ToUserLinkViewModel());

    [DisplayName("Анонс проекта")]
    public MarkupString ProjectAnnounce { get; } = projectDescription;

    public IReadOnlyCollection<ClaimLinkViewModel> MyClaims { get; } = claims;

    public bool HasCaptainAccess { get; } = captainAccessRules.Count > 0;

    [DisplayName("Название проекта")]
    public ProjectName ProjectName { get; } = project.ProjectName;

    public string Title => "Игра «" + ProjectName + "»";

    public IReadOnlyCollection<KogdaIgraCardViewModel> KogdaIgras { get; set; } = kogdaIgras;

    public bool DisableKogdaIgraMapping { get; set; } = disableKogdaIgraMapping;
}

public record class ProjectListItemViewModel(ProjectPersonalizedInfo p) : ProjectLinkViewModel(p.ProjectId, p.ProjectName)
{
    public bool IsMaster { get; } = p.HasMyMasterAccess;
    public bool IsActive { get; } = p.Active;
    public ProjectLifecycleStatus Status = p.ProjectLifecycleStatus;

    public bool PublishPlot { get; } = p.PublishPlot;

    [Display(Name = "Заявки открыты?")]
    public bool IsAcceptingClaims { get; } = p.IsAcceptingClaims;

    public bool HasMyClaims { get; } = p.HasMyClaims;
    public KogdaIgraIdentification? LastKogdaIgraId { get; set; } = p.LastKogdaIgraId;
}
