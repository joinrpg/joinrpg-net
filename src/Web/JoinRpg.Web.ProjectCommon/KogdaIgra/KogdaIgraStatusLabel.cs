namespace JoinRpg.Web.ProjectCommon.KogdaIgra;

public static class KogdaIgraStatusLabel
{
    /// <summary>
    /// Подпись статуса игры для списков выбора. Для нормальных статусов (OK и «Прошла») — null,
    /// чтобы не шуметь: подпись появляется, только когда есть о чём предупредить.
    /// </summary>
    public static string? ToWarningLabel(this KogdaIgraGameStatus? status) => status switch
    {
        null or KogdaIgraGameStatus.Ok or KogdaIgraGameStatus.Passed => null,
        KogdaIgraGameStatus.Unknown => "Статус неизвестен",
        KogdaIgraGameStatus.Postponed => "Отложена",
        KogdaIgraGameStatus.DateUncertain => "Дата уточняется",
        KogdaIgraGameStatus.Cancelled => "Отменена",
        _ => $"Статус {(int)status.Value}",
    };

    public static string ToSelectorSubtext(this KogdaIgraShortViewModel game)
    {
        var label = game.Status.ToWarningLabel();
        if (label is null)
        {
            return game.Year?.ToString() ?? "";
        }
        return game.Year is null ? label : $"{game.Year} · {label}";
    }
}
