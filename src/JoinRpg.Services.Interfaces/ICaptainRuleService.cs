using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Interfaces;

public interface ICaptainRuleService
{
    Task RemoveRule(CaptainAccessRule captainAccessSetting);
    Task AddOrChangeRule(CaptainAccessRule captainAccessSetting);
}
