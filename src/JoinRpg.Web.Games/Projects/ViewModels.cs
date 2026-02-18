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

public record ProjectListItemViewModel(
    bool IsMaster,
    bool IsActive,
    ProjectLifecycleStatus Status,
    bool PublishPlot,
    [property: Display(Name = "Заявки открыты?")] bool IsAcceptingClaims,
    bool HasMyClaims,
    KogdaIgraIdentification? LastKogdaIgraId,
    ProjectIdentification ProjectId,
    string ProjectName)
    : ProjectLinkViewModel(ProjectId, ProjectName)
{
    public ProjectListItemViewModel(ProjectPersonalizedInfo p) : this(p.HasMyMasterAccess, p.Active, p.ProjectLifecycleStatus, p.PublishPlot, p.IsAcceptingClaims, p.HasMyClaims, p.LastKogdaIgraId, p.ProjectId, p.ProjectName)
    {

    }

    public ProjectListItemViewModel(ProjectShortInfo p) : this(IsMaster: false, p.Active, p.ProjectLifecycleStatus, p.PublishPlot, p.IsAcceptingClaims, HasMyClaims: false,
        p.KiLinks?.OrderByDescending(k => k.End).FirstOrDefault()?.Id, p.ProjectId, p.ProjectName)
    {

    }
}
