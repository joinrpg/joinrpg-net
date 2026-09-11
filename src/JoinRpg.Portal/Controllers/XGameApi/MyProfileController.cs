using JoinRpg.Interfaces;
using JoinRpg.WebPortal.Managers.Projects;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-api/me")]
public class MyProfileController(IProjectApiViewService projectApiViewService, ICurrentUserAccessor currentUserAccessor) : XGameApiController
{

    /// <summary>
    /// All active projects that current user has access
    /// </summary>
    [HttpGet, Authorize("XApiUser"), Route("projects/active")]
    public async Task<IEnumerable<ProjectHeader>> GetActiveProjects()
    {
        return await projectApiViewService.GetActiveProjects(currentUserAccessor.UserIdentification);
    }
}
