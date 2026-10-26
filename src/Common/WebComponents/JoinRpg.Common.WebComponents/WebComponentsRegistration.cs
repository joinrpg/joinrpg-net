using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Common.WebComponents;

/// <summary>Регистрация сервисов, нужных компонентам библиотеки.</summary>
public static class WebComponentsRegistration
{
    /// <summary>Зарегистрировать сервисы компонентов. Вызывать в каждом хосте, где они рендерятся.</summary>
    public static IServiceCollection AddJoinRpgWebComponents(this IServiceCollection services)
        => services.AddScoped<SelectorListCache>();
}
