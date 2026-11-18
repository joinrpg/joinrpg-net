using JoinRpg.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.AdminTools.UserMerge;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>Админское слияние аккаунтов (ADR024).</summary>
[ApiController]
[Route("/webapi/UserMerge/[action]")]
[AdminAuthorize]
public class UserMergeController(IUserMergeClient client) : ControllerBase
{
    [HttpGet]
    public async Task<Results<Ok<UserMergePreviewViewModel>, BadRequest<string>>> GetPreview(string primaryUserLink, string secondaryUserLink)
    {
        try
        {
            return TypedResults.Ok(await client.GetPreview(primaryUserLink, secondaryUserLink));
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

    [HttpPost]
    public async Task<Results<Ok, BadRequest<string>>> Merge(UserIdentification primary, UserIdentification secondary)
    {
        try
        {
            await client.Merge(primary, secondary);
            return TypedResults.Ok();
        }
        catch (JoinRpgBaseException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
