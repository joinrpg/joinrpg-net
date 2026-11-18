namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Глобальное объявление над содержимым каждой страницы Portal и IdPortal
/// (например, «данные восстановлены из резервной копии»). Задаётся конфигурацией,
/// без админки: <c>SiteBanner__Text</c> в переменных окружения.
/// </summary>
public class SiteBannerOptions
{
    public const string SectionName = "SiteBanner";

    /// <summary>
    /// Текст объявления, простой текст без разметки. Пустой или отсутствующий — баннера нет.
    /// </summary>
    public string? Text { get; set; }
}
