using System.Text.RegularExpressions;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Services.Email;

public partial class MassProjectEmailService(
    IClaimsRepository claimsRepository,
    IProjectMetadataRepository projectMetadataRepository,
    INotificationService notificationService,
    ICurrentUserAccessor currentUserAccessor) : IMassProjectEmailService
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

        List<NotificationRecepient> recipients = CalculateRecepients(alsoMailToMasters, claims, project);

        var template = new NotificationEventTemplate(NamePlaceholderRegex().Replace(body.Contents, "%recepient.name%"));

        var notification = new NotificationEvent(NotificationClass.MassProjectEmails,
                                                 project.ProjectId, // Казалось бы это заявка, но нет, письмо к заявке не имеет отношения, оно всем рассылается
                                                 subject,
                                                 template,
                                                 recipients,
                                                 currentUserAccessor.UserIdentification);

        await notificationService.QueueNotification(notification);
    }

    internal static List<NotificationRecepient> CalculateRecepients(bool alsoMailToMasters, IReadOnlyCollection<ClaimWithPlayer> claims, ProjectInfo project)
    {
        List<NotificationRecepient> recipients = [
                    ..
            claims.Select(c =>NotificationRecepient.Player(c.Player)).DistinctBy(x => x.UserId) // Выкидываем повторные заявки
                    ];

        if (alsoMailToMasters)
        {
            foreach (var master in project.Masters)
            {
                if (!recipients.Any(x => x.UserId == master.UserId))
                {
                    // Если мастер уже получает письмо как игрок, не дублируем
                    recipients.Add(new NotificationRecepient(master));
                }
            }
        }

        return recipients;
    }

    [GeneratedRegex("%NAME%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NamePlaceholderRegex();

}
