using System.Net;

namespace JoinRpg.Portal.Infrastructure.Authentication;

public interface IRecaptchaVerificator
{
    Task<bool> ValidateToken(string recaptchaToken, IPAddress? clientIp);
    bool IsRecaptchaConfigured();
}
