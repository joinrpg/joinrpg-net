namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Ставит <see cref="LazyLoadObservingMiddleware"/> первым в пайплайн тестового хоста.
/// </summary>
internal sealed class LazyLoadObservingStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            _ = app.UseMiddleware<LazyLoadObservingMiddleware>();
            next(app);
        };
}
