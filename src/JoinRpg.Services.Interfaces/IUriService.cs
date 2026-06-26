using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.Services.Interfaces;

public interface IUriService
{
    string Get(ILinkable link);
    Uri GetUri(ILinkable link);
}
