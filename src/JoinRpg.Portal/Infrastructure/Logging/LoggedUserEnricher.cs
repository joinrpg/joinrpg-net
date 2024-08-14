using System.Security.Claims;
using Serilog.Core;
using Serilog.Events;

namespace JoinRpg.Portal.Infrastructure.Logging;

internal class LoggedUserEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public LoggedUserEnricher() : this(new HttpContextAccessor()) { }
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "LoggedUser", httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value));
    }
}

