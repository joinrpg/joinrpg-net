using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces.Email;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email;
internal class MasterEmailServiceImpl(IUriService uriService, IEmailSendingService messageService) : IMasterEmailService
{
    async Task IMasterEmailService.EmailProjectClosed(ProjectClosedMail email)
    {

        await messageService.SendEmail(email,
            $"{email.ProjectName}: проект закрыт",
            $@"Добрый день, {messageService.GetRecepientPlaceholderName()}

Проект {email.ProjectName} был закрыт. Вы всегда можете найти его по ссылке {uriService.GetUri(email.ProjectId)}

--
{email.Initiator.GetDisplayName()}

");
    }
}
