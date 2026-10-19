using Microsoft.AspNetCore.Diagnostics;

namespace JoinRpg.Common.WebInfrastructure;

public static class ErrorPageReExecuteExtensions
{
    /// <summary>
    /// Страницу ошибки, перезапущенную из <c>UseExceptionHandler</c> или
    /// <c>UseStatusCodePagesWithReExecute</c>, отрисовывать методом GET. Иначе она получает метод
    /// упавшего запроса, а страница Blazor на POST ищет отправленную форму (и падает, не найдя её)
    /// и вовсе не принимает DELETE, PUT и прочие. Ставить сразу после этих двух middleware.
    /// </summary>
    public static IApplicationBuilder UseErrorPageReExecuteAsGet(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (context.Features.Get<IExceptionHandlerFeature>() is null
                && context.Features.Get<IStatusCodeReExecuteFeature>() is null)
            {
                await next(context);
                return;
            }

            // Возвращаем исходный метод: по нему после запроса пишутся метрики и лог хостинга.
            var originalMethod = context.Request.Method;
            context.Request.Method = HttpMethods.Get;
            try
            {
                await next(context);
            }
            finally
            {
                context.Request.Method = originalMethod;
            }
        });
}
