namespace JoinRpg.WebComponents
{
    public record IntSelectListItem(
            int Value,
            string Text,
            string Subtext = "",
            string ExtraSearch = "",
            bool Disabled = false
        )
    {
    }
}
