using System.Net;
using JoinRpg.Portal.Infrastructure.Authentication;

namespace JoinRpg.IntegrationTests.TestInfrastructure.Stubs;

internal class StubRecaptchaVerificator : IRecaptchaVerificator
{
    Task<bool> IRecaptchaVerificator.ValidateToken(string recaptchaToken, IPAddress? clientIp) =>
        Task.FromResult(true);

    bool IRecaptchaVerificator.IsRecaptchaConfigured() => true;
}
