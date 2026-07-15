using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectCommon.ElementMoving;
using JoinRpg.WebComponents;

namespace JoinRpg.WebPortal.Managers;

internal class MoveViewService(IFieldSetupService fieldSetupService, ICharacterGroupService characterGroupService) : IMoveClient
{
    public async Task<string[]> MoveAfterAsync(string selfId, string parentId, string? moveAfterId)
    {
        if (!MoveRequest.TryParse(selfId, parentId, moveAfterId, out var request))
        {
            throw new ArgumentException($"Cannot parse IDs: selfId='{selfId}', parentId='{parentId}'");
        }

        return request switch
        {
            { SelfId: ProjectFieldIdentification fieldId, ParentId: ProjectIdentification projectId }
                when fieldId.ProjectId == projectId
                    && (request.MoveAfterId is null or ProjectFieldIdentification)
                => await MoveFieldAsync(fieldId, projectId, request.MoveAfterId as ProjectFieldIdentification),
            { SelfId: CharacterIdentification characterId, ParentId: CharacterGroupIdentification groupId }
                when characterId.ProjectId == groupId.ProjectId
                    && (request.MoveAfterId is null or CharacterIdentification)
                => await MoveCharacterAsync(groupId, characterId, request.MoveAfterId as CharacterIdentification),
            _ => throw new ArgumentException(
                $"Unsupported ID combination: selfId='{selfId}', parentId='{parentId}'"),
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

    private async Task<string[]> MoveCharacterAsync(
        CharacterGroupIdentification groupId,
        CharacterIdentification characterId,
        CharacterIdentification? afterCharacterId)
    {
        var sortedIds = await characterGroupService.MoveCharacterAfter(groupId, characterId, afterCharacterId);
        return [.. sortedIds.Select(id => id.ToString())];
    }
}
