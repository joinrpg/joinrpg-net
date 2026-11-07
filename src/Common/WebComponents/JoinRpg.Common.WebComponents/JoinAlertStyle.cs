namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Классы собственных стилей <see cref="JoinAlert"/> (см. JoinAlert.razor.css). Классов Bootstrap здесь нет намеренно.
/// </summary>
internal static class JoinAlertStyle
{
    public static string Build(VariationStyleEnum? variationStyle, bool withIcon)
    {
        var variationString = variationStyle switch
        {
            VariationStyleEnum.None or null => " join-alert--default",
            VariationStyleEnum.Primary => " join-alert--primary",
            VariationStyleEnum.Success => " join-alert--success",
            VariationStyleEnum.Info => " join-alert--info",
            VariationStyleEnum.Warning => " join-alert--warning",
            VariationStyleEnum.Danger => " join-alert--danger",
            _ => throw new ArgumentException("Incorrect variation", nameof(variationStyle)),
        };

        return "join-alert" + variationString + (withIcon ? " join-alert--with-icon" : "");
    }
}
