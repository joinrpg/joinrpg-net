using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Interfaces.Notifications;
public record NotificationMessage(
    NotificationClass NotificationClass,
    ProjectIdentification? Project,
    string Header,
    MarkdownString Text,
    NotificationRecepient[] Recepients,
    UserIdentification Initiator)
{
}
