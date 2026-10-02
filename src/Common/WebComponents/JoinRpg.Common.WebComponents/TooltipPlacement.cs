namespace JoinRpg.Common.WebComponents;

/// <summary>
/// С какой стороны от цели показывать подсказку <see cref="Tooltip"/>.
/// Если с этой стороны не хватает места, скрипт сам отзеркалит подсказку на противоположную.
/// </summary>
public enum TooltipPlacement
{
    /// <summary>Под целью.</summary>
    Bottom,

    /// <summary>Над целью.</summary>
    Top,

    /// <summary>Слева от цели.</summary>
    Left,

    /// <summary>Справа от цели.</summary>
    Right,
}
