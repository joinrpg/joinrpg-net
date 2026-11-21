using JoinRpg.DomainTypes;

namespace JoinRpg.Interfaces;

public record class BudgetOptions
{
    public required ProjectIdentification Project { get; set; }
}
