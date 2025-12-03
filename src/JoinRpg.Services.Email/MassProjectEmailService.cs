using System.Text.RegularExpressions;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Services.Email;

public partial class MassProjectEmailService(
    IClaimsRepository claimsRepository,
    IProjectMetadataRepository projectMetadataRepository,
    INotificationService notificationService,
    ICurrentUserAccessor currentUserAccessor,
    INotificationUriLocator<ClaimIdentification> uriLocator
    ) : IMassProjectEmailService
{
    public async Task MassMail(
        ClaimIdentification[] claimIds,
        MarkdownString body,
        string subject,
        bool alsoMailToMasters)
    {
        var claims = await claimsRepository.GetClaimHeadersWithPlayer(claimIds);
        if (claims.Count == 0)
        {
            return;
        }
        var project = await projectMetadataRepository.GetProjectMetadata(claimIds.First().ProjectId);
        _ = project.EnsureProjectActive();
        if (!project.HasMasterAccess(currentUserAccessor, Permission.CanSendMassMails))
        {
            claims = claims.Where(claim => claim.ResponsibleMasterUserId == currentUserAccessor.UserId).ToArray();
        }

        if (string.IsNullOrWhiteSpace(body.Contents))
        {
            throw new ArgumentException("Empty body", nameof(body));
        }

        List<NotificationRecepient> recipients = CalculateRecepients(claims, project);

        var template = new NotificationEventTemplate(
            ClaimPlaceholderRegex().Replace(
                NamePlaceholderRegex().Replace(body.Contents, "%recepient.name%"),
                "%recepient.claim%")
            );

        var notification = new NotificationEvent(NotificationClass.MassProjectEmails,
                                                 project.ProjectId, // Казалось бы это заявка, но нет, письмо к заявке не имеет отношения, оно всем рассылается
                                                 subject,
                                                 template,
                                                 recipients,
                                                 currentUserAccessor.UserIdentification);

        await notificationService.QueueNotification(notification);

        if (alsoMailToMasters)
        {
            await SendToAllMasters(project, template, subject, currentUserAccessor.UserIdentification, recipients);
        }
    }

    public async Task PlotEmail(ClaimIdentification[] claimIds, MarkdownString body, PlotElementIdentification plotElementId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(plotElementId.ProjectId);

        var claims = await claimsRepository.GetClaimHeadersWithPlayer(claimIds);
        if (claims.Count == 0)
        {
            return;
        }
        _ = project.EnsureProjectActive();

        var template = new NotificationEventTemplate($@"
Добрый день, %recepient.name%!

Для вас опубликована вводная. Прочитать ее: %recepient.claim%#pe{plotElementId.PlotElementId}

{body.Contents ?? ""}
");

        List<NotificationRecepient> recipients = CalculateRecepients(claims, project);

        var notification = new NotificationEvent(NotificationClass.MassProjectEmails,
                                                 plotElementId,
                                                 Header: $@"{project.ProjectName}: опубликована вводная",
                                                 template,
                                                 recipients,
                                                 currentUserAccessor.UserIdentification);

        await notificationService.QueueNotification(notification);
    }

    private async Task SendToAllMasters(ProjectInfo project, NotificationEventTemplate body, string subject, UserIdentification initiator, List<NotificationRecepient> players)
    {
        var masterReps = new List<NotificationRecepient>();
        foreach (var master in project.Masters)
        {
            if (!players.Any(x => x.UserId == master.UserId))
            {
                // Если мастер уже получает письмо как игрок, не дублируем
                masterReps.Add(new NotificationRecepient(master));
            }
        }
        var notification = new NotificationEvent(NotificationClass.MasterProject, project.ProjectId, subject, body, masterReps, initiator);
        await notificationService.QueueNotification(notification);

    }

    internal List<NotificationRecepient> CalculateRecepients(IReadOnlyCollection<ClaimWithPlayer> claims, ProjectInfo project)
    {
        return [.. claims.Select(ToNotificationRecepient).DistinctBy(x => x.UserId)]; // Выкидываем повторные заявки
    }

    private NotificationRecepient ToNotificationRecepient(ClaimWithPlayer c)
    {
        return NotificationRecepient.Player(c.Player).AddField("claim", uriLocator.GetUri(c.ClaimId).AbsoluteUri);
    }
    [GeneratedRegex("%NAME%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NamePlaceholderRegex();

    [GeneratedRegex("%CLAIM%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClaimPlaceholderRegex();
}
