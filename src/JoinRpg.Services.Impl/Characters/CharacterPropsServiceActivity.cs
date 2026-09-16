using System.Diagnostics;

namespace JoinRpg.Services.Impl.Characters;

/// <summary>
/// Источник трассировки операций над агрегатом персонажа. Имя регистрируется в настройке
/// телеметрии Portal, рядом с источником <c>ProjectPropsService</c>.
/// </summary>
public static class CharacterPropsServiceActivity
{
    public const string ActivitySourceName = nameof(CharacterPropsService);

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
