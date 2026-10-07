using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectMasterTools.Fields;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/project-field-operations/[action]")]
[IgnoreAntiforgeryToken]
[MasterAuthorize(Permission.CanChangeFields)]
public class ProjectFieldOperationsController(
    IFieldSetupService fieldSetupService,
    IProjectFieldOperationsClient fieldOperationsClient,
    IProjectMetadataRepository projectMetadataRepository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> CreateTimeSlots([FromQuery] ProjectIdentification projectId, [FromBody] TimeSlotMassAddRequest request)
    {
        if (request.FieldId.ProjectId != projectId)
        {
            return BadRequest();
        }

        await fieldOperationsClient.CreateTimeSlots(request);
        return Ok();
    }

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

    [HttpPost]
    public async Task<ActionResult> DeleteUnusedVariants([FromQuery] ProjectIdentification projectId, [FromBody] ProjectFieldIdentification fieldId)
    {
        if (fieldId.ProjectId != projectId)
        {
            return BadRequest();
        }

        _ = await fieldSetupService.DeleteUnusedFieldValueVariants(fieldId);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SortTimeSlotsByStartTime([FromQuery] ProjectIdentification projectId, [FromBody] ProjectFieldIdentification fieldId)
    {
        if (fieldId.ProjectId != projectId)
        {
            return BadRequest();
        }

        var metadata = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!metadata.GetFieldById(fieldId).IsTimeSlot)
        {
            return BadRequest();
        }

        await fieldOperationsClient.SortTimeSlotsByStartTime(fieldId);
        return Ok();
    }
}
