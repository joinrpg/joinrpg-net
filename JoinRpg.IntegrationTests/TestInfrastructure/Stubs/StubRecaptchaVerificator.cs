using System.Net;
using System.Threading.Tasks;
using JoinRpg.Portal.Infrastructure.Authentication;

namespace JoinRpg.IntegrationTests.TestInfrastructure.Stubs
{
    internal class StubRecaptchaVerificator : IRecaptchaVerificator
    {
        Task<bool> IRecaptchaVerificator.ValidateToken(string recaptchaToken, IPAddress clientIp) =>
            Task.FromResult(true);
    }
}
