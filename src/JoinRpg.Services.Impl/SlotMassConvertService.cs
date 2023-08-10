using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;

namespace JoinRpg.Services.Impl;
internal class SlotMassConvertService : DbServiceImplBase, ISlotMassConvertService
{
    private readonly ICharacterService characterService;

    public SlotMassConvertService(
        IUnitOfWork unitOfWork,
        ICurrentUserAccessor currentUserAccessor,
        ICharacterService characterService) : base(unitOfWork, currentUserAccessor)
    {
        this.characterService = characterService;
    }

    public async Task MassConvert(ProjectIdentification projectId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        var groups = project.CharacterGroups
                    .Where(cg => !cg.IsSpecial && (cg.HaveDirectSlots || cg.Claims.Any()))
                    .Select(cg => (cg.CharacterGroupId, cg.CharacterGroupName))
                    .ToList();
        foreach (var group in groups)
        {
            await characterService.CreateSlotFromGroup(projectId, group.CharacterGroupId, $"Заявки — {group.CharacterGroupName}", allowToChangeInactive: true);
        }
    }
}
