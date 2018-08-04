using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Email
{
    public interface IEmailSendingService
    {
        Task SendEmails(string subject,
            MarkdownString body,
            RecepientData sender,
            IReadOnlyCollection<RecepientData> to);

        Task SendEmails(string subject,
            string body,
            string text,
            RecepientData sender,
            IReadOnlyCollection<RecepientData> to);

        string GetRecepientPlaceholderName();
        string GetUserDependentValue(string valueKey);
    }
}
