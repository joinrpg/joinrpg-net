using System.Globalization;
using JoinRpg.DomainTypes;
using JoinRpg.Portal.Infrastructure.ModelBinding;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace JoinRpg.Portal.Test.Infrastructure.ModelBinding;

/// <summary>
/// Форма комментария (<c>AddCommentForm</c> в <c>JoinRpg.Web.Comments</c>) постит в
/// <c>ForumController.CreateComment</c> id обсуждения и родительского комментария голыми числами, а
/// <see cref="AddCommentViewModel"/> держит их typed id. Они не <c>IProjectEntityId</c> — собирает их
/// стандартный биндер через <c>TypeConverter</c> от <c>[TypedEntityId]</c>. Провайдер проектных id
/// подключён, как в <c>Startup</c>.
/// </summary>
public class AddCommentViewModelBindingTest
{
    private static readonly IServiceProvider Services =
        new ServiceCollection()
            .AddLogging()
            .AddMvc(options => options.ModelBinderProviders.Insert(0, new ProjectEntityIdModelBinderProvider()))
            .Services.BuildServiceProvider();

    private static async Task<(AddCommentViewModel Model, ModelStateDictionary ModelState)> BindAsync(
        params (string Key, string Value)[] fields)
    {
        var httpContext = new DefaultHttpContext { RequestServices = Services };
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);
        var form = fields.ToDictionary(f => f.Key, f => new StringValues(f.Value));
        var valueProvider = new FormValueProvider(BindingSource.Form, new FormCollection(form), CultureInfo.InvariantCulture);

        var metadataProvider = Services.GetRequiredService<IModelMetadataProvider>();
        var metadata = metadataProvider.GetMetadataForType(typeof(AddCommentViewModel));
        var binder = Services.GetRequiredService<IModelBinderFactory>()
            .CreateBinder(new ModelBinderFactoryContext { Metadata = metadata, CacheToken = metadata });

        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext, valueProvider, metadata, bindingInfo: null, modelName: "");
        await binder.BindModelAsync(bindingContext);

        bindingContext.Result.IsModelSet.ShouldBeTrue();
        return ((AddCommentViewModel)bindingContext.Result.Model!, modelState);
    }

    [Fact]
    public async Task Reply_BindsTypedIds()
    {
        var (model, modelState) = await BindAsync(
            ("ProjectId", "1620"), ("CommentDiscussionId", "42"), ("ParentCommentId", "77"), ("CommentText", "Ответ"));

        model.CommentDiscussionId.ShouldBe(new CommentDiscussionId(42));
        model.ParentCommentId.ShouldBe(new CommentId(77));
        modelState.ErrorCount.ShouldBe(0);
    }

    [Fact]
    public async Task TopLevelComment_HasNoParent()
    {
        var (model, modelState) = await BindAsync(
            ("ProjectId", "1620"), ("CommentDiscussionId", "42"), ("CommentText", "Комментарий"));

        model.CommentDiscussionId.ShouldBe(new CommentDiscussionId(42));
        model.ParentCommentId.ShouldBeNull();
        modelState.ErrorCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    public async Task GarbageDiscussionId_DoesNotBind(string value)
    {
        var (model, modelState) = await BindAsync(("ProjectId", "1620"), ("CommentDiscussionId", value));

        model.CommentDiscussionId.ShouldBeNull();
        modelState.ContainsKey(nameof(AddCommentViewModel.CommentDiscussionId)).ShouldBeTrue();
    }
}
