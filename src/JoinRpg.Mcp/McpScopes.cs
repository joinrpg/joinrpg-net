namespace JoinRpg.Mcp;

/// <summary>
/// Scope, которые принимает эндпоинт /mcp. Публикуются в Protected Resource Metadata
/// (RFC 9728 §2) — именно оттуда клиент узнаёт, что запрашивать у IdPortal.
/// </summary>
/// <remarks>
/// Значения обязаны совпадать с <c>JoinRpgScopes</c> в <c>JoinRpg.IdPortal.OAuthServer</c>.
/// Общего кода между Portal и IdPortal нет — это контракт токена, как и claim
/// <c>projects</c> в <see cref="McpAuthContext"/>. Если строк станет больше, стоит вынести
/// их в общий проект (например, JoinRpg.Common.WebInfrastructure, на который ссылаются обе
/// стороны), чтобы они не разъехались.
/// </remarks>
internal static class McpScopes
{
    public const string Read = "joinrpg.read";

    /// <summary>
    /// Scope на запись. В IdPortal он есть (ADR012), но /mcp его пока не использует:
    /// пишущих инструментов нет, все пять читающие.
    /// </summary>
    public const string CharactersWrite = "joinrpg.characters.write";

    /// <summary>
    /// Что публикуется в Protected Resource Metadata — только то, что сервер действительно
    /// умеет. Спека MCP называет <c>scopes_supported</c> минимальным набором для базовой
    /// работы, а остальное предписывает добирать по ходу: через <c>scope</c> в заголовке
    /// <c>WWW-Authenticate</c> у того инструмента, которому не хватило прав.
    /// </summary>
    /// <remarks>
    /// Рекламировать <see cref="CharactersWrite"/> раньше времени вредно с двух сторон. Клиент
    /// начинает просить у IdPortal права, которых у него может не быть, и тогда падает весь
    /// вход целиком (<c>invalid_request</c>, ID2051) — именно так и вышло при подключении
    /// Claude Code к dev. А пользователь видит на экране согласия «создание и правка
    /// персонажей» у сервера, который ничего не пишет.
    ///
    /// Когда появятся пишущие инструменты — добавить сюда.
    /// </remarks>
    public static readonly string[] Advertised = [Read];
}
