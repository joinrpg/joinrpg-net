using System.Globalization;
using JoinRpg.DomainTypes;
using JoinRpg.Portal.Infrastructure.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace JoinRpg.Portal.Test.Infrastructure.ModelBinding;

/// <summary>
/// <see cref="CommentId"/> — не <c>IProjectEntityId</c>, поэтому <c>ProjectEntityIdModelBinder</c> его
/// не берёт: MVC биндит его стандартным биндером простых типов через <c>TypeConverter</c>, который
/// генератор <c>[TypedEntityId]</c> вешает на тип. Форма скрытия комментария (<c>forums/concealcomment</c>)
/// постит голое число. Провайдер проектных id подключён, как в <c>Startup</c>.
/// </summary>
public class CommentIdModelBindingTest
{
    private static readonly IServiceProvider Services =
        new ServiceCollection()
            .AddLogging()
            .AddMvc(options => options.ModelBinderProviders.Insert(0, new ProjectEntityIdModelBinderProvider()))
            .Services.BuildServiceProvider();

    private static async Task<(ModelBindingResult Result, ModelStateDictionary ModelState)> BindAsync(string? value)
    {
        var httpContext = new DefaultHttpContext { RequestServices = Services };
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);
        var query = value is null
            ? []
            : new Dictionary<string, StringValues> { ["commentId"] = value };
        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query,
            new QueryCollection(query),
            CultureInfo.InvariantCulture);

        var metadataProvider = Services.GetRequiredService<IModelMetadataProvider>();
        var metadata = metadataProvider.GetMetadataForType(typeof(CommentId));
        var binder = Services.GetRequiredService<IModelBinderFactory>()
            .CreateBinder(new ModelBinderFactoryContext { Metadata = metadata, CacheToken = metadata });

        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext, valueProvider, metadata, bindingInfo: null, modelName: "commentId");
        await binder.BindModelAsync(bindingContext);
        return (bindingContext.Result, modelState);
    }

    [Fact]
    public async Task BareNumber_BindsToCommentId()
    {
        var (result, _) = await BindAsync("123");

        result.IsModelSet.ShouldBeTrue();
        result.Model.ShouldBe(new CommentId(123));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    public async Task Garbage_DoesNotBind(string value)
    {
        var (result, modelState) = await BindAsync(value);

        result.IsModelSet.ShouldBeFalse();
        modelState.ContainsKey("commentId").ShouldBeTrue();
    }

    [Fact]
    public async Task Missing_DoesNotBind()
    {
        var (result, modelState) = await BindAsync(null);

        result.IsModelSet.ShouldBeFalse();
        modelState.ContainsKey("commentId").ShouldBeFalse();
    }
}
