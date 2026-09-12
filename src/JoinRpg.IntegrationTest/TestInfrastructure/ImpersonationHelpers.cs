using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Interfaces;

namespace JoinRpg.IntegrationTests.TestInfrastructure;

/// <summary>
/// Помощники для выполнения кода от имени конкретного пользователя (impersonation) в интеграционных тестах.
/// </summary>
public static class ImpersonationHelpers
{
    private static readonly UserDisplayName DefaultDisplayName = new("Тестовый Пользователь", FullName: null);

    /// <summary>
    /// Выполняет действие от имени указанного пользователя в рамках уже созданного DI-scope.
    /// Гарантированно снимает impersonation после завершения.
    /// </summary>
    public static async Task<T> ImpersonateAsync<T>(
        this IServiceProvider scopedServices,
        UserIdentification userId,
        Func<Task<T>> action,
        UserDisplayName? displayName = null,
        bool isAdmin = false)
    {
        var impersonator = scopedServices.GetRequiredService<IImpersonateAccessor>();
        impersonator.StartImpersonate(userId, displayName ?? DefaultDisplayName, isAdmin);
        try
        {
            return await action();
        }
        finally
        {
            impersonator.StopImpersonate();
        }
    }

    /// <inheritdoc cref="ImpersonateAsync{T}(IServiceProvider, UserIdentification, Func{Task{T}}, UserDisplayName?, bool)"/>
    public static Task ImpersonateAsync(
        this IServiceProvider scopedServices,
        UserIdentification userId,
        Func<Task> action,
        UserDisplayName? displayName = null,
        bool isAdmin = false)
        => scopedServices.ImpersonateAsync<object?>(
            userId,
            async () =>
            {
                await action();
                return null;
            },
            displayName,
            isAdmin);

    /// <summary>
    /// Создаёт свежий DI-scope (как новый запрос), выполняет в нём действие от имени указанного пользователя
    /// и возвращает результат. Подходит для последовательности операций разными пользователями.
    /// </summary>
    public static async Task<T> RunAsAsync<T>(
        this IServiceProvider rootServices,
        UserIdentification userId,
        Func<IServiceProvider, Task<T>> action,
        UserDisplayName? displayName = null,
        bool isAdmin = false)
    {
        using var scope = rootServices.CreateScope();
        return await scope.ServiceProvider.ImpersonateAsync(
            userId,
            () => action(scope.ServiceProvider),
            displayName,
            isAdmin);
    }

    /// <inheritdoc cref="RunAsAsync{T}(IServiceProvider, UserIdentification, Func{IServiceProvider, Task{T}}, UserDisplayName?, bool)"/>
    public static Task RunAsAsync(
        this IServiceProvider rootServices,
        UserIdentification userId,
        Func<IServiceProvider, Task> action,
        UserDisplayName? displayName = null,
        bool isAdmin = false)
        => rootServices.RunAsAsync<object?>(
            userId,
            async sp =>
            {
                await action(sp);
                return null;
            },
            displayName,
            isAdmin);
}
