namespace JoinRpg.XGameApi.Contract;

/// <summary>Лёгкая карточка персонажа для списков (MCP <c>list_characters</c>) — по id потом зовут get_characters.</summary>
public class CharacterListItem
{
    public int CharacterId { get; set; }

    public required string CharacterName { get; set; }

    public bool IsActive { get; set; }
}
