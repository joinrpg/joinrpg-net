using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

/// <summary>
/// Что надо записать по итогам сохранения полей. Стратегии сами сущности не трогают — результат
/// применяет <see cref="FieldSaveHelper"/>.
/// </summary>
/// <param name="UpdatedFields">Поля, значение которых изменилось.</param>
/// <param name="Character">
/// <c>null</c>, если персонаж не меняется: сохранение идёт в неутверждённую заявку.
/// </param>
/// <param name="ClaimFields">
/// Итоговый слой полей заявки; <c>null</c>, если заявки нет и писать некуда.
/// </param>
public record class FieldSaveResult(
    IReadOnlyCollection<FieldWithPreviousAndNewValue> UpdatedFields,
    CharacterUpdate? Character,
    FieldLayerContainer? ClaimFields);

/// <summary>Изменения персонажа по итогам сохранения полей.</summary>
/// <param name="Fields">Итоговый слой полей персонажа.</param>
/// <param name="Description">
/// <c>null</c> — описание не трогать: у проекта не настроено поле описания персонажа.
/// Пустое описание — это не <c>null</c>, а <see cref="MarkdownString"/> с пустой строкой.
/// </param>
public record class CharacterUpdate(
    FieldLayerContainer Fields,
    string CharacterName,
    MarkdownString? Description,
    IReadOnlyCollection<CharacterGroupIdentification> ParentGroupIds);
