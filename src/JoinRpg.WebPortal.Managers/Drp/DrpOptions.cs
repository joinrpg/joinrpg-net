namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Параметры восстановления после потери основной БД (DRP, #5481), секция <c>Drp</c>.
/// </summary>
public record class DrpOptions
{
    /// <summary>Момент аварии: уведомления после него к восстанавливаемому состоянию не относятся.</summary>
    public required DateTimeOffset LostAt { get; set; }

    /// <summary>Момент резервной копии, из которой восстановлена база: события до него уже в ней учтены.</summary>
    public required DateTimeOffset BackupTakenAt { get; set; }
}
