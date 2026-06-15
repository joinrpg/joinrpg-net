using JoinRpg.DomainTypes.Plots;

namespace JoinRpg.Services.Interfaces.Notification;

public interface IMassProjectEmailService
{
    Task MassMail(ClaimIdentification[] claimIds, MarkdownDbValue body, string subject, bool alsoMailToMasters);
    Task PlotEmail(ClaimIdentification[] claimIds, MarkdownDbValue body, PlotElementIdentification plotElementId);
}
