using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/project-field-operations/[action]")]
[IgnoreAntiforgeryToken]
[MasterAuthorize(Permission.CanChangeFields)]
public class ProjectFieldOperationsController(IFieldSetupService fieldSetupService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Delete([FromQuery] ProjectIdentification projectId, [FromBody] ProjectFieldIdentification fieldId)
    {
        if (fieldId.ProjectId != projectId)
        {
            return BadRequest();
        }

        await fieldSetupService.DeleteField(projectId.Value, fieldId.ProjectFieldId);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> DeleteVariant([FromQuery] ProjectIdentification projectId, [FromBody] ProjectFieldVariantIdentification variantId)
    {
        if (variantId.FieldId.ProjectId != projectId)
        {
            return BadRequest();
        }

        _ = await fieldSetupService.DeleteFieldValueVariant(projectId.Value, variantId.FieldId.ProjectFieldId, variantId.ProjectFieldVariantId);
        return Ok();
    }
}
