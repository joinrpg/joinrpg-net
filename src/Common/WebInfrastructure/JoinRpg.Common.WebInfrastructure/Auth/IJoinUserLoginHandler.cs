using System.Security.Claims;
using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Common.WebInfrastructure.Auth;

/// <summary>
/// Приложение-специфичная часть логина через id.joinrpg.ru: сохраняет/обновляет пользователя
/// и дополняет claims, с которыми он будет залогинен по cookie-схеме.
/// </summary>
public interface IJoinUserLoginHandler
{
    Task HandleLoginAsync(UserIdentification userId, ClaimsPrincipal externalPrincipal, List<Claim> claims, CancellationToken cancellationToken);
}
