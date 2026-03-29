using System.Text.Json;

namespace JoinRpg.XGameApi.Contract;

/// <summary>
/// Запрос на создание нового персонажа
/// </summary>
public class CreateCharacterRequest
{
    /// <summary>
    /// Тип персонажа
    /// </summary>
    public CharacterTypeApi CharacterType { get; set; } = CharacterTypeApi.Player;

    /// <summary>
    /// Горячий персонаж — показывается в первую очередь при поиске (только для типов Player и Slot)
    /// </summary>
    public bool IsHot { get; set; } = false;

    /// <summary>
    /// Видимость персонажа
    /// </summary>
    public CharacterVisibilityApi CharacterVisibility { get; set; } = CharacterVisibilityApi.Public;

    /// <summary>
    /// Максимальное количество заявок на слот (только для типа Slot; null — без ограничений)
    /// </summary>
    public int? SlotLimit { get; set; }

    /// <summary>
    /// Название слота (только для типа Slot)
    /// </summary>
    public string? SlotName { get; set; }

    /// <summary>
    /// Значения полей персонажа. Ключ — идентификатор поля, значение — JSON-значение поля
    /// </summary>
    public Dictionary<int, JsonElement> FieldValues { get; set; } = new();
}

/// <summary>
/// Тип персонажа
/// </summary>
public enum CharacterTypeApi
{
    /// <summary>
    /// Персонаж для игрока — принимает заявки
    /// </summary>
    Player,

    /// <summary>
    /// Внеигровой персонаж (NPC) — заявки не принимает
    /// </summary>
    NonPlayer,

    /// <summary>
    /// Слот — персонаж-шаблон, допускающий несколько заявок
    /// </summary>
    Slot,
}

/// <summary>
/// Видимость персонажа для участников
/// </summary>
public enum CharacterVisibilityApi
{
    /// <summary>
    /// Персонаж виден всем (игрокам и мастерам)
    /// </summary>
    Public,

    /// <summary>
    /// Персонаж виден только владельцу заявки и мастерам
    /// </summary>
    PlayerHidden,

    /// <summary>
    /// Персонаж виден только мастерам
    /// </summary>
    Private,
}
