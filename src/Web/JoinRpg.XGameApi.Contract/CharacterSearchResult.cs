namespace JoinRpg.XGameApi.Contract;

/// <summary>Лёгкий результат поиска персонажа — по нему потом вызывают get_characters пачкой.</summary>
public class CharacterSearchResult
{
    public int CharacterId { get; set; }

    public required string CharacterName { get; set; }

    public bool IsPerfectMatch { get; set; }
}
