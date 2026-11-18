using JoinRpg.Common.WebComponents;
using JoinRpg.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Резолв строки «ссылка/id/telegram/email» в пользователя для <c>JoinUserLinkEditor</c>.
/// </summary>
/// <remarks>
/// Доступен любому авторизованному пользователю: резолв нужен всем, кто вводит ссылку, а не
/// только мастерам. Обратная сторона — оракул перебора: ответ подтверждает факт регистрации по
/// чужому email или телеграму. Риск принят осознанно — сузить его правами всё равно не выходит,
/// проект на joinrpg заводится в два клика.
///
/// В ответ уходят только id и отображаемое имя — контакты через этот эндпоинт не отдаются ни в
/// каком виде, и это проверяется тестом.
/// </remarks>
[ApiController]
[Route("/webapi/user-link/[action]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class UserLinkResolveController(IUserLinkResolveClient client) : ControllerBase
{
    [HttpPost]
    public async Task<Results<Ok<UserLinkViewModel>, BadRequest<string>>> Resolve(string userLink)
    {
        try
        {
            return TypedResults.Ok(await client.ResolveUserLink(userLink));
        }
        catch (FormatException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (JoinRpgBaseException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
