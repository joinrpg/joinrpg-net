using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebComponents;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure;

internal class UriServiceImpl(
    IHttpContextAccessor httpContextAccessor,
    LinkGenerator linkGenerator,
    IOptions<NotificationsOptions> notificationOptions) : IUriService,
    IUriLocator<UserLinkViewModel>,
    IUriLocator<CharacterGroupLinkSlimViewModel>,
    IUriLocator<CharacterLinkSlimViewModel>,
    IUriLocator<ProjectIdentification>,
    IProjectUriLocator,
    ICharacterUriLocator,
    IUriLocator<PlotFolderIdentification>,
    IUriLocator<ClaimIdentification>,
    IUriLocator<ClaimCommentIdentification>,
    IUriLocator<ForumThreadIdentification>,
    IUriLocator<CharacterIdentification>,
    IUriLocator<CharacterGroupIdentification>,
    IUriLocator<PlotElementIdentification>,
    IUriLocator<PlotVersionIdentification>,
    IUriLocator<ProjectFieldIdentification>,
    IUriLocator<ProjectFieldVariantIdentification>,
    IUriLocator<PaymentTypeIdentification>,
    IUriLocator<FinanceOperationIdentification>,
    IUriLocator<ForumCommentIdentification>,
    IUriLocator<ProjectRolesListIdentification>,
    INotificationEntityLinkRenderer
{
    public Uri GetUri(ILinkable linkable)
    {
        ArgumentNullException.ThrowIfNull(linkable);

        var linkType = linkable.LinkType;
        var projectId = linkable.ProjectId;
        var identification = linkable.Identification;

        var link = linkType switch
        {
            LinkType.ResultUser => linkGenerator.GetPathByAction("Details",
                                "User",
                                new { UserId = identification }),
            LinkType.ResultCharacterGroup => linkGenerator.GetPathByAction("Details",
                                "GameGroups",
                                new { CharacterGroupId = identification, ProjectId = projectId }),
            LinkType.CharacterGroupRoles => linkGenerator.GetPathByAction("Index",
                                "GameGroups",
                                new { CharacterGroupId = identification, ProjectId = projectId }),
            LinkType.ResultCharacter => linkGenerator.GetPathByAction("Details",
                                "Character",
                                new { CharacterId = identification, ProjectId = projectId }),
            LinkType.Claim => linkGenerator.GetPathByAction("Edit",
                                "Claim",
                                new { ProjectId = projectId, ClaimId = identification }),
            LinkType.Plot => linkGenerator.GetPathByAction("Edit",
                                "Plot",
                                new { PlotFolderId = identification, ProjectId = projectId }),
            LinkType.Comment => linkGenerator.GetPathByAction("ToComment",
                                "DiscussionRedirect",
                                new { ProjectId = projectId, CommentId = identification }),
            LinkType.CommentDiscussion => linkGenerator.GetPathByAction("ToDiscussion",
                                "DiscussionRedirect",
                                new { ProjectId = projectId, CommentDiscussionId = identification }),
            LinkType.Project => linkGenerator.GetPathByAction("Details", "Game", new { ProjectId = projectId }),

            LinkType.PaymentSuccess when linkable is ILinkableClaim lc =>
                                linkGenerator.GetPathByAction("ClaimPaymentSuccess", "Payments", new { projectId, claimId = lc.ClaimId }),

            LinkType.PaymentFail when linkable is ILinkableClaim lc
                                => linkGenerator.GetPathByAction("ClaimPaymentFail", "Payments", new { projectId, claimId = lc.ClaimId }),

            LinkType.PaymentUpdate when linkable is ILinkablePayment lp
                                => linkGenerator.GetPathByAction("UpdateClaimPayment", "Payments", new { projectId = lp.ProjectId, claimId = lp.ClaimId, orderId = lp.OperationId }),
            _ => throw new ArgumentOutOfRangeException(nameof(linkType)),
        };

        if (link is null)
        {
            throw new InvalidOperationException($"Failed to create link to {linkable}");
        }
        Uri baseDomain = GetBaseDomain();

        return new Uri(baseDomain, link);
    }

    private Uri GetBaseDomain()
    {
        Uri baseDomain;
        if (httpContextAccessor.HttpContext?.Request is HttpRequest request)
        {
            // внутри веб реквеста, берем схему и хост из него
            baseDomain = new Uri($"{request.Scheme}://{request.Host}");
        }
        else
        {
            // Берем из настроек
            baseDomain = notificationOptions.Value.BaseDomain;
        }

        return baseDomain;
    }

    public string Get(ILinkable link) => GetUri(link).AbsoluteUri;

    Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target) =>
        GetUri(new Linkable(LinkType.ResultUser, ProjectId: null, Identification: target.UserId.ToString()));
    Uri IUriLocator<CharacterGroupLinkSlimViewModel>.GetUri(CharacterGroupLinkSlimViewModel target) =>
         GetUri(new Linkable(target.CharacterGroupId));
    Uri IUriLocator<CharacterLinkSlimViewModel>.GetUri(CharacterLinkSlimViewModel target)
        => GetUri(new Linkable(target.CharacterId));
    public Uri GetUri(ProjectIdentification target) => GetUri(new Linkable(target));
    Uri IProjectUriLocator.GetMyClaimUri(ProjectIdentification projectId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("MyClaim", "Claim", new { ProjectId = projectId.Value }));
    Uri IProjectUriLocator.GetAddClaimUri(ProjectIdentification projectId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("AddForGroup", "Claim", new { ProjectId = projectId.Value }));
    public Uri GetDetailsUri(CharacterIdentification characterId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("Details", "Character", new { CharacterId = characterId.CharacterId, ProjectId = characterId.ProjectId.Value }));
    public Uri GetAddClaimUri(CharacterIdentification characterId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("AddForCharacter", "Claim", new { CharacterId = characterId.CharacterId, ProjectId = characterId.ProjectId.Value }));
    Uri IProjectUriLocator.GetCreatePlotUri(ProjectIdentification projectId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("Create", "Plot", new { ProjectId = projectId.Value }));
    public Uri GetUri(PlotFolderIdentification target) => GetUri(new Linkable(target));
    Uri IProjectUriLocator.GetRolesListUri(ProjectIdentification projectId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("Index", "GameGroups", new { ProjectId = projectId.Value }));
    public Uri GetUri(ClaimIdentification target) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("Edit", "Claim", new { ProjectId = target.ProjectId.Value, target.ClaimId }));

    public Uri GetUri(ForumThreadIdentification target) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("ViewThread", "Forum", new { ProjectId = target.ProjectId.Value, ForumThreadId = target.ThreadId }));

    public Uri GetUri(ClaimCommentIdentification target) =>
        new(GetUri(target.ClaimId).AbsoluteUri + $"#comment{target.CommentId}");
    Uri IProjectUriLocator.GetCaptainCabinetUri(ProjectIdentification projectId) => new(GetBaseDomain(), linkGenerator.GetPathByPage("/GamePages/CaptainCabinet", values: new { ProjectId = projectId.Value }));

    public Uri GetUri(CharacterIdentification target) => GetUri(new Linkable(target));

    public Uri GetUri(CharacterGroupIdentification target) => GetUri(new Linkable(target));

    public Uri GetUri(PlotElementIdentification target) =>
        new(GetBaseDomain(), linkGenerator.GetPathByAction("EditElement", "Plot", new { ProjectId = target.ProjectId.Value, elementId = target }));

    public Uri GetUri(PlotVersionIdentification target) =>
        new(GetBaseDomain(), linkGenerator.GetPathByAction("ShowElementVersion", "Plot",
            new { ProjectId = target.ProjectId.Value, target.PlotFolderId.PlotFolderId, target.PlotElementId.PlotElementId, Version = target.Version }));

    public Uri GetUri(ProjectFieldIdentification target) =>
        new(GetBaseDomain(), linkGenerator.GetPathByAction("Edit", "GameField", new { ProjectId = target.ProjectId.Value, target.ProjectFieldId }));

    public Uri GetUri(ProjectFieldVariantIdentification target) =>
        new(GetBaseDomain(), linkGenerator.GetPathByAction("EditValue", "GameField",
            new { ProjectId = target.ProjectId.Value, target.FieldId.ProjectFieldId, valueId = target.ProjectFieldVariantId }));

    public Uri GetUri(PaymentTypeIdentification target) =>
        new(GetBaseDomain(), linkGenerator.GetPathByAction("EditPaymentType", "Finances", new { ProjectId = target.ProjectId.Value, target.PaymentTypeId }));

    public Uri GetUri(FinanceOperationIdentification target) =>
        new(GetBaseDomain(), linkGenerator.GetPathByAction("ToFinanceOperation", "DiscussionRedirect",
            new { ProjectId = target.ProjectId.Value, target.FinanceOperationId }));

    public Uri GetUri(ForumCommentIdentification target) =>
        new(GetUri(target.ThreadId).AbsoluteUri + $"#comment{target.CommentId}");

    public Uri GetUri(ProjectRolesListIdentification target) =>
        new(GetBaseDomain(), linkGenerator.GetPathByPage("/GamePages/ProjectRoleListView",
            values: new { ProjectId = target.ProjectId.Value, Id = target.ProjectRolesListId }));

    RenderedEntityLink? INotificationEntityLinkRenderer.RenderEntityLink(IProjectEntityId? entityReference)
    {
        // Поддерживаемые типы сущностей уведомлений. Остальные → null (ссылка не добавляется).
        (Uri? uri, string? name) = entityReference switch
        {
            ClaimCommentIdentification c => (GetUri(c), "комментарий"),
            ClaimIdentification c => (GetUri(c), "заявка"),
            ForumCommentIdentification c => (GetUri(c), "сообщение на форуме"),
            ForumThreadIdentification t => (GetUri(t), "обсуждение"),
            FinanceOperationIdentification f => (GetUri(f), "финансовая операция"),
            ProjectIdentification p => (GetUri(p), "проект"),
            _ => (null, null),
        };

        return uri is not null && name is not null
            ? new RenderedEntityLink(
                Markdown: new MarkdownString($"Подробнее: [{name}]({uri})"),
                PlainText: $"Подробнее: {name}: {uri}")
            : null;
    }

    private record Linkable(LinkType LinkType, int? ProjectId, string? Identification) : ILinkable
    {
        public Linkable(LinkType linkType, IProjectEntityId projectEntityId) : this(linkType, projectEntityId.ProjectId, projectEntityId.Id.ToString())
        {

        }

        public Linkable(CharacterIdentification id) : this(LinkType.ResultCharacter, id) { }

        public Linkable(ProjectIdentification id) : this(LinkType.Project, id.Value, Identification: null) { }

        public Linkable(CharacterGroupIdentification id) : this(LinkType.CharacterGroupRoles, id) { }

        public Linkable(PlotFolderIdentification id) : this(LinkType.Plot, id) { }
    }
}
