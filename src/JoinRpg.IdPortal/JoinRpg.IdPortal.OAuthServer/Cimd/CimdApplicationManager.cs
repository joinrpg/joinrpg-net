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
    ICimdMetadataLoader loader)
    : OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication>(cache, logger, options, store)
{
    public override async ValueTask<OpenIddictEntityFrameworkCoreApplication?> FindByClientIdAsync(
        string identifier, CancellationToken cancellationToken = default)
    {
        if (!CimdClientId.TryParse(identifier, out var clientId))
        {
            return await base.FindByClientIdAsync(identifier, cancellationToken);
        }

        var document = await loader.LoadAsync(clientId, cancellationToken);
        if (document is null)
        {
            // §4.3: не смогли получить и проверить документ — клиента считаем неизвестным.
            return null;
        }

        // Строка в БД может уже существовать (её заводит EnsurePersistedAsync после согласия),
        // но содержимое всё равно берём из свежего документа: иначе redirect_uri, удалённый
        // клиентом из документа, продолжал бы приниматься по устаревшей строке.
        var application = await base.FindByClientIdAsync(identifier, cancellationToken)
            ?? await Store.InstantiateAsync(cancellationToken);

        await PopulateAsync(application, BuildDescriptor(clientId, document), cancellationToken);
        return application;
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

        var document = await loader.LoadAsync(clientId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var descriptor = BuildDescriptor(clientId, document);
        var existing = await base.FindByClientIdAsync(identifier, cancellationToken);

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
    private static OpenIddictApplicationDescriptor BuildDescriptor(CimdClientId clientId, CimdDocument document)
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

        foreach (var uri in document.ValidRedirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }

        return descriptor;
    }
}
