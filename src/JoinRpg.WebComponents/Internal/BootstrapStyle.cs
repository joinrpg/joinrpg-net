namespace JoinRpg.WebComponents;

internal static class BootstrapStyle
{
    public static string Build(string componentPrefix, VariationStyleEnum? variationStyle, SizeStyleEnum? size)
    {
        var variationString = variationStyle switch
        {
            VariationStyleEnum.Success => $" {componentPrefix}-success",
            VariationStyleEnum.None => $" {componentPrefix}-default",
            null => $" {componentPrefix}-default",
            VariationStyleEnum.Warning => $" {componentPrefix}-warning",
            VariationStyleEnum.Danger => $" {componentPrefix}-danger",
            _ => throw new ArgumentException("Incorrect variation", nameof(variationStyle)),
        };

        var sizeString = size switch
        {
            SizeStyleEnum.Large => $" {componentPrefix}-lg",
            SizeStyleEnum.Medium => $"",
            null => "",
            SizeStyleEnum.Small => $" {componentPrefix}-sm",
            _ => throw new ArgumentException("Incorrect style", nameof(size)),
        };

        return componentPrefix + variationString + sizeString;
    }
}
