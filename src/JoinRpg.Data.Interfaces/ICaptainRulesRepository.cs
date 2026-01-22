using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Data.Interfaces;

public interface ICaptainRulesRepository
{
    Task<IReadOnlyCollection<CaptainAccessRule>> GetCaptainRules(ProjectIdentification projectIdentification);
    Task<IReadOnlyCollection<CaptainAccessRule>> GetCaptainRules(ProjectIdentification projectIdentification, UserIdentification userId);
}
