namespace JoinRpg.WebComponents;

public record TypedSelectListItem<TKey>(
        TKey Value,
        string Text,
        string Subtext = "",
        string ExtraSearch = "",
        bool Disabled = false
    ) where TKey : notnull
{

}

public static class TypedSelectList
{
    // Для type inference
    public static TypedSelectListItem<TKey> CreateItem<TKey>(TKey Value, string Text, string Subtext = "", string ExtraSearch = "", bool Disabled = false)
    where TKey : notnull
    => new TypedSelectListItem<TKey>(Value, Text, Subtext, ExtraSearch, Disabled);
}
