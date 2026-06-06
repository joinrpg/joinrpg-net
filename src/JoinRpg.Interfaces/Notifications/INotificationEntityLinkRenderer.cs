using JoinRpg.DomainTypes;

namespace JoinRpg.Interfaces.Notifications;

/// <summary>
/// Строит ссылку на сущность, к которой относится уведомление,
/// для добавления в текст письма/телеграм-сообщения джобами отправки.
/// </summary>
public interface INotificationEntityLinkRenderer
{
    /// <summary>
    /// Возвращает ссылку на сущность уведомления или <c>null</c>,
    /// если ссылки нет или тип сущности не поддержан.
    /// </summary>
    RenderedEntityLink? RenderEntityLink(IProjectEntityId? entityReference);
}

/// <summary>
/// Готовая ссылка на сущность в двух формах: markdown (для HTML/Telegram, где она
/// превращается в кликабельную) и plain text (для plain-text писем, где важно
/// сохранить видимый URL — markdown-ссылка в plain-text теряет адрес).
/// </summary>
public sealed record RenderedEntityLink(string Markdown, string PlainText);
