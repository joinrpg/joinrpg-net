namespace JoinRpg.Tools.Drp.Common;

/// <summary>
/// Ключи конфигурации DRP-инструментов. Строки подключения — с теми же именами, что в Portal.
/// </summary>
public static class DrpKeys
{
    public const string LostAt = "Drp:LostAt";
    public const string ReportDir = "Drp:ReportDir";

    /// <summary>Полный путь к отчёту — только разовое переопределение из командной строки (--report).</summary>
    public const string ReportPath = "Drp:ReportPath";

    /// <summary>Основная БД (MSSQL).</summary>
    public const string MainConnectionName = "DefaultConnection";

    /// <summary>База уведомлений (Postgres).</summary>
    public const string NotificationsConnectionName = "Notifications";

    /// <summary>Максимальный id таблицы в восстановленном бэкапе, например Drp:BackupMax:Users.</summary>
    public static string BackupMax(string table) => $"Drp:BackupMax:{table}";

    public static string ConnectionString(string name) => $"ConnectionStrings:{name}";
}
