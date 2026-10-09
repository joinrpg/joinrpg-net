using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// Страница подтверждения удаления персонажа. Строится по доменному агрегату, а не по EF-сущности:
/// признак шаблона по умолчанию с сущности тянул ленивую догрузку настроек проекта (#5112).
/// </summary>
public class DeleteCharacterViewModel(CharacterInfo character)
{
    public CharacterIdentification Id { get; } = character.Id;
    public string CharacterName { get; } = character.CharacterName;
    public bool HasActiveClaims { get; } = character.HasActiveClaims;
    public bool IsDefaultTemplate { get; } = character.ProjectInfo.ClaimSettings.DefaultTemplate == character.Id;
}
