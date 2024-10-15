using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces;

public class EditProjectRequest
{
    public required ProjectIdentification ProjectId { get; set; }
    public required string ProjectName { get; set; }
    public required string ClaimApplyRules { get; set; }
    public required string ProjectAnnounce { get; set; }
    public bool IsAcceptingClaims { get; set; }
    public required bool MultipleCharacters { get; set; }
    public required bool PublishPlot { get; set; }
    public required bool AutoAcceptClaims { get; set; }
    public required bool IsAccommodationEnabled { get; set; }

    public required CharacterIdentification? DefaultTemplateCharacterId { get; set; }
}
