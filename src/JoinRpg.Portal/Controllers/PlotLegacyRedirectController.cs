using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

/// <summary>Перенаправляет старые URL /{projectId}/plot/... на /{projectId}/plots/...</summary>
[Route("{projectId}/plot")]
public class PlotLegacyRedirectController : Controller
{
    [HttpGet("{**path}")]
    public IActionResult LegacyRedirect(int projectId, string? path = null)
        => RedirectPermanent($"/{projectId}/plots/{path}");
}
