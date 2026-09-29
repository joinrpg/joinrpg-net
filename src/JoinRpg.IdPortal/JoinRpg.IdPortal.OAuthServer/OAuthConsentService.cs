using JoinRpg.Common.PrimitiveTypes;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// Записывает решение пользователя со страницы согласия (ADR012 §4) как постоянную
/// авторизацию OpenIddict. Выдавать согласие имеет право только сама страница: она
/// защищена antiforgery-токеном, а <c>connect/authorize</c> решение из запроса не принимает
/// и просто ищет уже записанную авторизацию.
/// </summary>
public interface IOAuthConsentService
{
    /// <param name="clientId">Клиент, которому пользователь открывает доступ.</param>
    /// <param name="user">Пользователь, выдающий согласие.</param>
    /// <param name="scopes">Права, которые были показаны пользователю на странице.</param>
    /// <param name="projectIds">Выбранные проекты. Пустой список — согласие без доступа к проектам.</param>
    Task GrantAsync(
        string clientId,
        UserIdentification user,
        IReadOnlyCollection<string> scopes,
        IReadOnlyCollection<int> projectIds,
        CancellationToken ct = default);
}

internal class OAuthConsentService(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager) : IOAuthConsentService
{
    public async Task GrantAsync(
        string clientId,
        UserIdentification user,
        IReadOnlyCollection<string> scopes,
        IReadOnlyCollection<int> projectIds,
        CancellationToken ct = default)
    {
        var application = await applicationManager.FindByClientIdAsync(clientId, ct)
            ?? throw new InvalidOperationException($"Клиент {clientId} не найден.");
        var applicationId = await applicationManager.GetIdAsync(application, ct)
            ?? throw new InvalidOperationException($"У клиента {clientId} нет идентификатора.");

        var descriptor = new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = applicationId,
            // Тот же формат subject, что и в connect/authorize, иначе выданное согласие там не найдётся.
            Subject = user.ToString(),
            Type = AuthorizationTypes.Permanent,
            Status = Statuses.Valid,
        };

        foreach (var scope in scopes)
        {
            descriptor.Scopes.Add(scope);
        }

        OAuthConsent.StoreGrantedProjects(descriptor.Properties, projectIds);

        _ = await authorizationManager.CreateAsync(descriptor, ct);
    }
}
