namespace JoinRpg.WebComponents;

public record EntitySelectorItem<TKey>(
    TKey Key,
    string Text,
    string Subtext = "",
    string ExtraSearch = "",
    bool Disabled = false
)
    where TKey : notnull;
