namespace JoinRpg.Interfaces;

/// <summary>Настройки джобы ежемесячного финансового отчёта в Telegram (секция "BudgetMonthlyReport").
/// Биндинг — вручную в Startup (см. конфигурацию BudgetMonthlyReportOptions): TelegramChatId — типизированный
/// идентификатор, из конфигурации читается как long.</summary>
public record class BudgetMonthlyReportOptions
{
    /// <summary>Chat id Telegram-канала для отчёта (отрицательный для каналов/супергрупп).</summary>
    public required TelegramChatId TelegramChatId { get; set; }

    /// <summary>Режим отправки: Daily — каждый день (отладка), Monthly — 16-го числа в 12:00 МСК.</summary>
    public BudgetMonthlyReportSchedule Schedule { get; set; } = BudgetMonthlyReportSchedule.Daily;
}

public enum BudgetMonthlyReportSchedule
{
    Daily,
    Monthly,
}
