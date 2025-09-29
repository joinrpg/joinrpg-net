using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Portal.Menu;

public record class DonateOptions
{
    public required int DonateProjectId { get; set; }

    public ProjectIdentification DonateProject => new ProjectIdentification(DonateProjectId);
}
