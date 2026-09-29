namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Резолв введённой строки (ссылка на профиль, id, vk, telegram, email) в пользователя сайта.
/// </summary>
/// <remarks>
/// Регистрация в DI необязательна: <see cref="JoinUserLinkEditor"/> берёт клиент мягко и без него
/// работает как обычный ввод строк.
/// <para>
/// Ответ намеренно ограничен <see cref="UserLinkViewModel"/> — id и отображаемое имя. Контакты
/// (email, телефон, соцсети) через этот канал не отдаются никогда: упоминание в поле не даёт
/// доступа к контактам (ADR017 §6).
/// </para>
/// </remarks>
public interface IUserLinkResolveClient
{
    /// <summary>
    /// Кидает исключение с понятным пользователю текстом, если пользователь не найден
    /// или строка не разобрана.
    /// </summary>
    Task<UserLinkViewModel> ResolveUserLink(string userLink);
}
