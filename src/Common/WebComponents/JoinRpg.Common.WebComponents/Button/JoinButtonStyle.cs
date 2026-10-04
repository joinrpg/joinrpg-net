namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Классы собственных стилей <see cref="JoinButton"/> (см. JoinButton.razor.css). Классов Bootstrap здесь нет намеренно.
/// </summary>
internal static class JoinButtonStyle
{
    public static string Build(VariationStyleEnum? variationStyle, SizeStyleEnum? size)
    {
        var variationString = variationStyle switch
        {
            VariationStyleEnum.None or null => " join-btn--default",
            VariationStyleEnum.Primary => " join-btn--primary",
            VariationStyleEnum.Success => " join-btn--success",
            VariationStyleEnum.Info => " join-btn--info",
            VariationStyleEnum.Warning => " join-btn--warning",
            VariationStyleEnum.Danger => " join-btn--danger",
            _ => throw new ArgumentException("Incorrect variation", nameof(variationStyle)),
        };

        var sizeString = size switch
        {
            SizeStyleEnum.Medium or null => "",
            SizeStyleEnum.Large => " join-btn--lg",
            SizeStyleEnum.Small => " join-btn--sm",
            SizeStyleEnum.ExtraSmall => " join-btn--xs",
            _ => throw new ArgumentException("Incorrect size", nameof(size)),
        };

        return "join-btn" + variationString + sizeString;
    }
}
