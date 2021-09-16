namespace JoinRpg.WebComponents
{
    internal static class BootstrapStyle
    {
        public static string Build(string componentPrefix, VariationStyleEnum? variationStyle, SizeStyleEnum? size)
        {
            var variationString = variationStyle switch
            {
                VariationStyleEnum.Success => $" {componentPrefix}-success",
                VariationStyleEnum.None => "",
                null => "",
                VariationStyleEnum.Warning => $" {componentPrefix}-warning",
            };

            var sizeString = size switch
            {
                SizeStyleEnum.Large => $" {componentPrefix}-lg",
                SizeStyleEnum.Medium => $"",
                null => "",
                SizeStyleEnum.Small => $" {componentPrefix}-sm",
            };

            return componentPrefix + variationString + sizeString;
        }
    }
}
