namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Резолв строки, введённой мастером (ссылка на профиль, id, vk, telegram, email), в пользователя
/// сайта — для редактора поля типа UserLink (ADR017 §5).
/// </summary>
/// <remarks>
/// Ответ намеренно ограничен <see cref="UserLinkViewModel"/> — id и отображаемое имя. Контакты
/// (email, телефон, соцсети) через этот канал не отдаются никогда: упоминание в поле не даёт
/// доступа к контактам (ADR017 §6).
/// <para>
/// Проект передаётся как <see cref="int"/>, а не типизированным id: эта библиотека компонентов
/// намеренно не зависит от <c>JoinRpg.DomainTypes</c> (так же сделан <see cref="IMoveClient"/>).
/// </para>
/// </remarks>
public interface IUserLinkResolveClient
{
    /// <summary>
    /// Кидает исключение с понятным пользователю текстом, если пользователь не найден
    /// или строка не разобрана.
    /// </summary>
    Task<UserLinkViewModel> ResolveUserLink(int projectId, string userLink);
}
