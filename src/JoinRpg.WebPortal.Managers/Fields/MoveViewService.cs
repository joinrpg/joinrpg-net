using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon.ElementMoving;

namespace JoinRpg.WebPortal.Managers.Fields;

internal class MoveViewService(IFieldSetupService fieldSetupService) : IMoveClient
{
    public async Task<string[]> MoveAfterAsync(MoveRequest request)
    {
        return request switch
        {
            { SelfId: ProjectFieldIdentification fieldId, ParentId: ProjectIdentification projectId }
                when fieldId.ProjectId == projectId
                    && (request.MoveAfterId is null or ProjectFieldIdentification)
                => await MoveFieldAsync(fieldId, projectId, request.MoveAfterId as ProjectFieldIdentification),
            _ => throw new ArgumentException(
                $"Unsupported ID combination: selfId='{request.SelfId}', parentId='{request.ParentId}'"),
        };
    }

    private async Task<string[]> MoveFieldAsync(
        ProjectFieldIdentification fieldId,
        ProjectIdentification projectId,
        ProjectFieldIdentification? moveAfterId)
    {
        var sortedIds = await fieldSetupService.MoveFieldAfter(projectId, fieldId.ProjectFieldId, moveAfterId?.ProjectFieldId);
        return [.. sortedIds.Select(id => id.ToString())];
    }
}
