namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// Страница подтверждения удаления персонажа.
/// </summary>
/// <param name="IsDefaultTemplate">Персонаж — шаблон по умолчанию проекта; считается по метаданным
/// проекта, а не по EF-сущности, иначе настройки проекта догружаются лениво (#5112).</param>
public record DeleteCharacterViewModel(
    int ProjectId,
    string CharacterName,
    bool HasActiveClaims,
    bool IsDefaultTemplate);
