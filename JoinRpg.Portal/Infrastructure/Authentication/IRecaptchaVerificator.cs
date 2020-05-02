using System.Net;
using System.Threading.Tasks;

namespace JoinRpg.Portal.Infrastructure.Authentication
{
    public interface IRecaptchaVerificator
    {
        Task<bool> ValidateToken(string recaptchaToken, IPAddress clientIp);
    }
}
