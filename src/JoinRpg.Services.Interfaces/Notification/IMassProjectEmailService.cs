using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.Services.Interfaces.Notification;

public interface IMassProjectEmailService
{
    Task MassMail(ClaimIdentification[] claimIds, MarkdownString body, string subject, bool alsoMailToMasters);
    Task PlotEmail(ClaimIdentification[] claimIds, MarkdownString body, PlotElementIdentification plotElementId);
}
