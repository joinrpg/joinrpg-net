namespace JoinRpg.Web.Games.FieldSetup;

public record GameFieldVariantDisplayItem(
    string Label,
    bool IsActive,
    CharacterGroupIdentification? CharacterGroup);
