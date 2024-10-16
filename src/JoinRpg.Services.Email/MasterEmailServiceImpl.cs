using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Email;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Email;
internal class MasterEmailServiceImpl(
    IUriService uriService,
    IEmailSendingService messageService,
    IProjectMetadataRepository projectMetadataRepository,
    IOptions<NotificationsOptions> options,
    IUserRepository userRepository
    ) : IMasterEmailService
{
    private readonly RecepientData joinRpgSender = options.Value.ServiceRecepient;

    public async Task EmailProjectStale(ProjectStaleMail email)
    {
        var metadata = await projectMetadataRepository.GetMastersList(email.ProjectId);

        var subject = $"{metadata.ProjectName}: проект будет закрыт из-за неактивности";

        var body = $@"Добрый день, {messageService.GetRecepientPlaceholderName()}

Проект {metadata.ProjectName} был в последний раз активен {email.LastActiveDate:yyyy-MM-dd}. Если до {email.WillCloseDate:yyyy-MM-dd} активность в нем не появится, он автоматически будет закрыт.

Подробности в справке https://docs.joinrpg.ru/ru/latest/project/after.html#id3

Не переживайте, закрытый проект всегда можно будет посмотреть, он не пропадет. Если проект завершен или больше не нужен, вы можете закрыть его сами.

Вы всегда можете найти его по ссылке {uriService.GetUri(email.ProjectId)}

--
{joinRpgSender.DisplayName}

";
        await SendToAllMasters(messageService, metadata, body, subject, joinRpgSender);
    }

    async Task IMasterEmailService.EmailProjectClosed(ProjectClosedMail email)
    {
        var initiator = await userRepository.GetById(email.Initiator.Value);

        var metadata = await projectMetadataRepository.GetMastersList(email.ProjectId);

        var body = $@"Добрый день, {messageService.GetRecepientPlaceholderName()}

Проект {metadata.ProjectName} был закрыт. Вы всегда можете найти его по ссылке {uriService.GetUri(email.ProjectId)}

--
{initiator.GetDisplayName()}

";
        var subject = $"{metadata.ProjectName}: проект закрыт";
        await SendToAllMasters(messageService, metadata, body, subject, initiator.ToRecepientData());
    }

    private static async Task SendToAllMasters(IEmailSendingService messageService, ProjectMastersListInfo metadata, string body, string subject, RecepientData initiator)
    {
        await messageService.SendEmails(
                    subject,
                    new MarkdownString(body),
                    initiator,
                    metadata.Masters.Select(master => new RecepientData(master)).ToArray()
                    );
    }

    async Task IMasterEmailService.EmailProjectClosedStale(ProjectClosedStaleMail email)
    {
        var metadata = await projectMetadataRepository.GetMastersList(email.ProjectId);

        var body = $@"Добрый день, {messageService.GetRecepientPlaceholderName()}

Проект {metadata.ProjectName} был закрыт, т.к. он не был активен с {email.LastActiveDate:yyyy-MM-dd}. Вы всегда можете найти его по ссылке {uriService.GetUri(email.ProjectId)}

--
{joinRpgSender.DisplayName}

";
        var subject = $"{metadata.ProjectName}: проект закрыт";
        await SendToAllMasters(messageService, metadata, body, subject, joinRpgSender);
    }

    async Task IMasterEmailService.EmalProjectNotUsingSlots(ProjectNotUsingSlots email)
    {
        var metadata = await projectMetadataRepository.GetMastersList(email.ProjectId);

        var subject = $"{metadata.ProjectName}: проект использует устаревший функционал";

        var body = $@"Добрый день, {messageService.GetRecepientPlaceholderName()}

Проект [{metadata.ProjectName}]({uriService.GetUri(email.ProjectId)}) использует устаревший функционал — заявки в группу. Он будет отключен в ноябре 2024 года. Вместо них сейчас используются шаблоны персонажей.
Больше об этом можно почитать по ссылке https://docs.joinrpg.ru/ru/latest/characters/slots.html

Вы можете автоматически сконвертировать все заявки в группу в шаблоны персонажей в Настройках проекта, или сконвертировать группы по одной в настройках. Никакие данные не потеряются.

Если проект завершен или больше не нужен — вы можете закрыть его, и эти сообщения перестанут приходить.

--
{joinRpgSender.DisplayName}

";
        await SendToAllMasters(messageService, metadata, body, subject, joinRpgSender);
    }

}
