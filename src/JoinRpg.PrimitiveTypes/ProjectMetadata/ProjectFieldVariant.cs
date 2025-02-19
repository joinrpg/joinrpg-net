using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public record class ProjectFieldVariant(
    ProjectFieldVariantIdentification Id,
    string Label,
    int Price,
    bool IsPlayerSelectable,
    bool IsActive,
    CharacterGroupIdentification? CharacterGroupId,
    MarkdownString Description,
    MarkdownString MasterDescription,
    string? ProgrammaticValue
    ) : IOrderableEntity
{
    int IOrderableEntity.Id => Id.ProjectFieldVariantId;
}
