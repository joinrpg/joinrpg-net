using System.Text.Json;
using JoinRpg.Common.WebInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.OAuthServer.Cimd;

/// <summary>
/// Резолвит клиентов, чей <c>client_id</c> — HTTPS-URL (CIMD, ADR012 §3). Обычные клиенты
/// идут в базу как и раньше.
/// </summary>
/// <remarks>
/// Почему подменяется именно менеджер: все его методы виртуальные, геттеры EF-стора читают
/// свойства сущности в памяти (без запроса в БД), а обработчики OpenIddict и на authorize,
/// и на token сходятся в <see cref="FindByClientIdAsync"/>. Токен-эндпоинт не passthrough —
/// другого способа повлиять на резолв клиента там нет.
/// </remarks>
public class CimdApplicationManager(
    IOpenIddictApplicationCache<OpenIddictEntityFrameworkCoreApplication> cache,
    ILogger<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>> logger,
    IOptionsMonitor<OpenIddictCoreOptions> options,
    IOpenIddictApplicationStore<OpenIddictEntityFrameworkCoreApplication> store,
    ICimdMetadataLoader loader,
    IOptions<JoinRpgHostNamesOptions> hostNames)
    : OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>(cache, logger, options, store)
{
    /// <summary>
    /// Метка в <c>Properties</c>: строку завёл CIMD, а не администратор. Ключ с namespace —
    /// Properties общие для всех, кто трогает приложение.
    /// </summary>
    private const string OriginPropertyName = "ru.joinrpg.cimd.origin";

    /// <summary>
    /// Сверка <c>redirect_uri</c>. Поверх точного совпадения разрешаем расхождение **только в
    /// порте** и **только для loopback**.
    /// </summary>
    /// <remarks>
    /// RFC 8252 §7.3 требует этого от сервера: нативное приложение получает свободный порт от
    /// операционной системы в момент запроса и заранее его не знает, поэтому в своих метаданных
    /// объявляет адрес без порта. Claude Code так и делает — объявляет
    /// <c>http://localhost/callback</c>, а приходит с <c>http://localhost:43575/callback</c>.
    /// При точной сверке такой клиент не подключится вовсе.
    ///
    /// Схема, хост и путь по-прежнему обязаны совпадать: <c>localhost</c> и <c>127.0.0.1</c>
    /// остаются разными адресами, а к не-loopback адресам послабление не применяется.
    /// </remarks>
    public override async ValueTask<bool> ValidateRedirectUriAsync(
        OpenIddictEntityFrameworkCoreApplication application,
        string address,
        CancellationToken cancellationToken = default)
    {
        if (await base.ValidateRedirectUriAsync(application, address, cancellationToken))
        {
            return true;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var requested) || !requested.IsLoopback)
        {
            return false;
        }

        foreach (var registered in await GetRedirectUrisAsync(application, cancellationToken))
        {
            if (Uri.TryCreate(registered, UriKind.Absolute, out var candidate)
                && candidate.IsLoopback
                && string.Equals(candidate.Scheme, requested.Scheme, StringComparison.Ordinal)
                && string.Equals(candidate.Host, requested.Host, StringComparison.OrdinalIgnoreCase)
                && string.Equals(candidate.AbsolutePath, requested.AbsolutePath, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public override async ValueTask<OpenIddictEntityFrameworkCoreApplication?> FindByClientIdAsync(
        string identifier, CancellationToken cancellationToken = default)
    {
        if (!CimdClientId.TryParse(identifier, out var clientId))
        {
            return await base.FindByClientIdAsync(identifier, cancellationToken);
        }

        var existing = await base.FindByClientIdAsync(identifier, cancellationToken);
        if (existing is not null && !await IsCimdOriginAsync(existing, cancellationToken))
        {
            // Регистрация администратора главнее: HTTPS-URL в client_id бывает и у обычного
            // клиента (так у resource server идентификатором становится URI ресурса). Иначе
            // владелец этого URL диктовал бы администратору redirect_uri, тип клиента и права.
            return existing;
        }

        var document = await loader.LoadAsync(clientId, cancellationToken);
        if (document is null)
        {
            // §4.3: не смогли получить и проверить документ — клиента считаем неизвестным.
            // Откатываться на свою же строку нельзя: она может быть устаревшей.
            return null;
        }

        // Своя строка в БД может уже существовать (её заводит EnsurePersistedAsync после
        // согласия), но содержимое всё равно берём из свежего документа: иначе redirect_uri,
        // удалённый клиентом из документа, продолжал бы приниматься по устаревшей строке.
        var application = existing ?? await Store.InstantiateAsync(cancellationToken);

        await PopulateAsync(application, BuildDescriptor(clientId, document), cancellationToken);
        return application;
    }

    private async ValueTask<bool> IsCimdOriginAsync(
        OpenIddictEntityFrameworkCoreApplication application, CancellationToken cancellationToken)
    {
        var properties = await GetPropertiesAsync(application, cancellationToken);
        return properties.TryGetValue(OriginPropertyName, out var marker)
            && marker.ValueKind is JsonValueKind.True;
    }

    /// <summary>
    /// Заводит (или обновляет) строку в БД для CIMD-клиента и возвращает её идентификатор.
    /// </summary>
    /// <remarks>
    /// Вызывается только после аутентификации пользователя — см. AuthorizeMethod. Анонимный
    /// запрос на /connect/authorize резолвится синтетической сущностью и в БД ничего не пишет,
    /// иначе кто угодно мог бы засорять таблицу приложений, подставляя произвольные URL.
    ///
    /// Строка нужна, потому что у Authorizations и Tokens внешний ключ на таблицу приложений:
    /// без неё выданные токены не связались бы с клиентом, а значит не работали бы ни отзыв,
    /// ни список подключённых приложений.
    /// </remarks>
    public async Task<string?> EnsurePersistedAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (!CimdClientId.TryParse(identifier, out var clientId))
        {
            return null;
        }

        var existing = await base.FindByClientIdAsync(identifier, cancellationToken);
        if (existing is not null && !await IsCimdOriginAsync(existing, cancellationToken))
        {
            // Строку администратора не трогаем — она главнее, см. FindByClientIdAsync. Иначе
            // документ по этому URL переписывал бы её прямо в БД, а не только в памяти.
            return await GetIdAsync(existing, cancellationToken);
        }

        var document = await loader.LoadAsync(clientId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var descriptor = BuildDescriptor(clientId, document);

        if (existing is null)
        {
            var created = await CreateAsync(descriptor, cancellationToken);
            return await GetIdAsync(created, cancellationToken);
        }

        await UpdateAsync(existing, descriptor, cancellationToken);
        return await GetIdAsync(existing, cancellationToken);
    }

    /// <summary>
    /// CIMD-клиент всегда публичный (симметричные секреты документ иметь не может) и получает
    /// ровно те же права, что и обычный MCP-клиент, — кроме redirect_uri, которые берутся
    /// из документа.
    /// </summary>
    private OpenIddictApplicationDescriptor BuildDescriptor(CimdClientId clientId, CimdDocument document)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId.Value,
            ClientSecret = null,
            DisplayName = document.ClientName,
            ClientType = ClientTypes.Public,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess,
            },
        };

        foreach (var scope in JoinRpgScopes.All)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        // MCP-клиент присылает resource (RFC 8707) безусловно — так требует спека MCP.
        // Без права rsrc: OpenIddict обрывает authorize с invalid_target.
        descriptor.Permissions.Add(
            Permissions.Prefixes.Resource + JoinRpgResources.Mcp(hostNames.Value));

        foreach (var uri in document.ValidRedirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }

        // По этой метке FindByClientIdAsync отличает свою строку от регистрации администратора,
        // которую переопределять нельзя.
        descriptor.Properties[OriginPropertyName] = JsonSerializer.SerializeToElement(true);

        return descriptor;
    }
}
