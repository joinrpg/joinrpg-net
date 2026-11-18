using System.Reflection;
using System.Text.Json;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.Portal.Controllers.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Резолв ссылки на пользователя для редактора поля UserLink (ADR017 §5, §6).
/// </summary>
public class UserLinkResolveControllerTest
{
    /// <summary>
    /// Анониму резолв недоступен: он подтверждает факт регистрации по чужому контакту
    /// (ADR017 §6). Авторизованным доступен всем — поле заполняет и игрок (§3).
    /// </summary>
    [Fact]
    public void ControllerIsNotOpenToAnonymous()
    {
        var type = typeof(UserLinkResolveController);

        type.GetCustomAttribute<AuthorizeAttribute>().ShouldNotBeNull();
        type.GetCustomAttribute<AllowAnonymousAttribute>().ShouldBeNull();

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            method.GetCustomAttribute<AllowAnonymousAttribute>()
                .ShouldBeNull($"Метод {method.Name} открыт анонимам, а резолв должен требовать входа");
        }
    }

    [Fact]
    public async Task ResolvedUserIsReturned()
    {
        var controller = new UserLinkResolveController(
            new FakeClient(new UserLinkViewModel(new UserIdentification(42), "Вася Пупкин", ViewMode.Show)));

        var result = await controller.Resolve("vk.com/id456");

        var ok = result.Result.ShouldBeOfType<Ok<UserLinkViewModel>>();
        ok.Value.ShouldNotBeNull();
        ok.Value.UserId.ShouldBe(new UserIdentification(42));
        ok.Value.DisplayName.ShouldBe("Вася Пупкин");
    }

    /// <summary>
    /// «Не найден» — понятный текст в ответе, а не 500.
    /// </summary>
    [Fact]
    public async Task NotFoundUserBecomesBadRequestWithMessage()
    {
        var controller = new UserLinkResolveController(
            new FakeClient(new FormatException("Пользователь с Telegram @vasya не найден.")));

        var result = await controller.Resolve("@vasya");

        var badRequest = result.Result.ShouldBeOfType<BadRequest<string>>();
        badRequest.Value.ShouldBe("Пользователь с Telegram @vasya не найден.");
    }

    /// <summary>
    /// Инвариант ADR017 §6: через этот канал не уходят контакты — только id и отображаемое имя.
    /// Проверяем именно текст ответа: подмена модели на «более красивую»
    /// (<c>UserProfileDetailsViewModel</c> отдаёт email, телефон и соцсети) сломала бы это молча.
    /// </summary>
    [Fact]
    public async Task ResponseCarriesNoContacts()
    {
        var controller = new UserLinkResolveController(
            new FakeClient(new UserLinkViewModel(new UserIdentification(42), "Вася Пупкин", ViewMode.Show)));

        var result = await controller.Resolve("42");

        var ok = result.Result.ShouldBeOfType<Ok<UserLinkViewModel>>();
        var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement;

        var propertyNames = json.EnumerateObject().Select(p => p.Name).ToList();
        propertyNames.ShouldBe(["UserId", "DisplayName", "ViewMode"], ignoreOrder: true);
    }

    private sealed class FakeClient : IUserLinkResolveClient
    {
        private readonly UserLinkViewModel? result;
        private readonly Exception? exception;

        public FakeClient(UserLinkViewModel result) => this.result = result;

        public FakeClient(Exception exception) => this.exception = exception;

        public Task<UserLinkViewModel> ResolveUserLink(string userLink)
            => exception is not null ? throw exception : Task.FromResult(result!);
    }
}
