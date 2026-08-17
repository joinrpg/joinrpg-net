using System.Globalization;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Portal.Infrastructure.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace JoinRpg.Portal.Test.Infrastructure.ModelBinding;

public class ProjectEntityIdModelBinderTest
{
    private static readonly ModelMetadataProvider MetadataProvider =
        (ModelMetadataProvider)new ServiceCollection().AddLogging().AddMvc().Services
            .BuildServiceProvider()
            .GetRequiredService<IModelMetadataProvider>();

    private static readonly ProjectIdentification CurrentProjectId = new(100);

    private static Task<ModelBindingResult> BindAsync(bool allowAnotherProject, string? value, ProjectIdentification? currentProjectId)
        => BindAsync<CharacterIdentification>(allowAnotherProject, value, currentProjectId, "characterId");

    private static async Task<ModelBindingResult> BindAsync<T>(bool allowAnotherProject, string? value, ProjectIdentification? currentProjectId, string modelName)
        where T : class, IProjectEntityId, ISpanParsable<T>
    {
        var httpContext = new DefaultHttpContext();
        if (currentProjectId is not null)
        {
            httpContext.Items["ProjectId"] = currentProjectId.Value;
        }
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var query = value is null
            ? new Dictionary<string, StringValues>()
            : new Dictionary<string, StringValues> { [modelName] = value };
        var valueProvider = new QueryStringValueProvider(BindingSource.Query, new QueryCollection(query), CultureInfo.InvariantCulture);
        var modelMetadata = MetadataProvider.GetMetadataForType(typeof(T));
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext, valueProvider, modelMetadata, bindingInfo: null, modelName: modelName);

        var binder = new ProjectEntityIdModelBinder<T>(allowAnotherProject);
        await binder.BindModelAsync(bindingContext);
        return bindingContext.Result;
    }

    [Fact]
    public async Task PlainNumber_ShouldMapToCurrentProject()
    {
        var result = await BindAsync(allowAnotherProject: false, "5", CurrentProjectId);

        result.IsModelSet.ShouldBeTrue();
        result.Model.ShouldBe(new CharacterIdentification(CurrentProjectId, 5));
    }

    [Fact]
    public async Task FullString_SameProject_ShouldBind()
    {
        var id = new CharacterIdentification(CurrentProjectId, 5);

        var result = await BindAsync(allowAnotherProject: false, id.ToString(), CurrentProjectId);

        result.IsModelSet.ShouldBeTrue();
        result.Model.ShouldBe(id);
    }

    [Fact]
    public async Task FullString_AnotherProject_WithoutAttribute_ShouldFail()
    {
        var id = new CharacterIdentification(new ProjectIdentification(999), 5);

        var result = await BindAsync(allowAnotherProject: false, id.ToString(), CurrentProjectId);

        result.IsModelSet.ShouldBeFalse();
    }

    [Fact]
    public async Task FullString_AnotherProject_WithAttribute_ShouldBindWithProjectFromString()
    {
        var id = new CharacterIdentification(new ProjectIdentification(999), 5);

        var result = await BindAsync(allowAnotherProject: true, id.ToString(), CurrentProjectId);

        result.IsModelSet.ShouldBeTrue();
        result.Model.ShouldBe(id);
    }

    [Fact]
    public async Task PlainNumber_WithAttribute_ShouldFail()
    {
        var result = await BindAsync(allowAnotherProject: true, "5", CurrentProjectId);

        result.IsModelSet.ShouldBeFalse();
    }

    [Fact]
    public async Task GarbageValue_ShouldFail()
    {
        var result = await BindAsync(allowAnotherProject: false, "not-an-id", CurrentProjectId);

        result.IsModelSet.ShouldBeFalse();
    }

    [Fact]
    public async Task PlainNumber_NoCurrentProject_ShouldFail()
    {
        var result = await BindAsync(allowAnotherProject: false, "5", currentProjectId: null);

        result.IsModelSet.ShouldBeFalse();
    }

    [Fact]
    public async Task FullString_NoCurrentProject_ShouldBind()
    {
        var id = new CharacterIdentification(new ProjectIdentification(999), 5);

        var result = await BindAsync(allowAnotherProject: false, id.ToString(), currentProjectId: null);

        result.IsModelSet.ShouldBeTrue();
        result.Model.ShouldBe(id);
    }

    [Fact]
    public async Task FullString_NoCurrentProject_WithAttribute_ShouldBind()
    {
        var id = new CharacterIdentification(new ProjectIdentification(999), 5);

        var result = await BindAsync(allowAnotherProject: true, id.ToString(), currentProjectId: null);

        result.IsModelSet.ShouldBeTrue();
        result.Model.ShouldBe(id);
    }

    // PlotElementIdentification вложен на два уровня (ProjectId -> PlotFolderId -> PlotElementId),
    // у него нет конструктора (ProjectIdentification, int) — голым числом его смёржить с текущим
    // проектом нельзя, но полный идентификатор должен по-прежнему биндиться.
    [Fact]
    public async Task NestedId_PlainNumber_ShouldFail()
    {
        var result = await BindAsync<PlotElementIdentification>(
            allowAnotherProject: false, "5", CurrentProjectId, "plotElementId");

        result.IsModelSet.ShouldBeFalse();
    }

    [Fact]
    public async Task NestedId_FullString_SameProject_ShouldBind()
    {
        var id = new PlotElementIdentification(new PlotFolderIdentification(CurrentProjectId, 2), 5);

        var result = await BindAsync<PlotElementIdentification>(
            allowAnotherProject: false, id.ToString(), CurrentProjectId, "plotElementId");

        result.IsModelSet.ShouldBeTrue();
        result.Model.ShouldBe(id);
    }

    // Регрессия: провайдер создаёт биндер через Activator.CreateInstance(binderType, [allowAnotherProject]).
    // У Activator.CreateInstance(Type, object) есть омоним Activator.CreateInstance(Type, bool) — если передать
    // allowAnotherProject не через object[]-перегрузку, компилятор выберет её и тихо вызовет parameterless
    // конструктор (которого нет), из-за чего провайдер либо падает с MissingMethodException, либо (если бы
    // parameterless ctor существовал) молча терял значение allowAnotherProject. Эти тесты идут через
    // реальный Provider.GetBinder, а не через `new ProjectEntityIdModelBinder<T>(...)` напрямую.
    private static readonly ProjectEntityIdModelBinderProvider Provider = new();

    private static IModelBinder GetBinderViaProvider<T>(bool allowAnotherProject)
        where T : class, IProjectEntityId, ISpanParsable<T>
    {
        var method = typeof(ProviderTestActions).GetMethod(
            allowAnotherProject ? nameof(ProviderTestActions.WithAttribute) : nameof(ProviderTestActions.WithoutAttribute))!;
        var parameterInfo = method.GetParameters().Single(p => p.ParameterType == typeof(T));
        var modelMetadata = MetadataProvider.GetMetadataForParameter(parameterInfo);
        var providerContext = new TestModelBinderProviderContext(modelMetadata, MetadataProvider);

        var binder = Provider.GetBinder(providerContext);
        binder.ShouldNotBeNull();
        return binder;
    }

    [Fact]
    public async Task Provider_WithoutAttribute_CreatesWorkingBinder()
    {
        var binder = GetBinderViaProvider<CharacterIdentification>(allowAnotherProject: false);
        var httpContext = new DefaultHttpContext();
        httpContext.Items["ProjectId"] = CurrentProjectId.Value;
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query, new QueryCollection(new Dictionary<string, StringValues> { ["characterId"] = "5" }), CultureInfo.InvariantCulture);
        var modelMetadata = MetadataProvider.GetMetadataForType(typeof(CharacterIdentification));
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext, valueProvider, modelMetadata, bindingInfo: null, modelName: "characterId");

        await binder.BindModelAsync(bindingContext);

        bindingContext.Result.IsModelSet.ShouldBeTrue();
        bindingContext.Result.Model.ShouldBe(new CharacterIdentification(CurrentProjectId, 5));
    }

    [Fact]
    public async Task Provider_WithAttribute_CreatesBinderThatActuallyHonoursAllowAnotherProject()
    {
        var binder = GetBinderViaProvider<CharacterIdentification>(allowAnotherProject: true);
        var id = new CharacterIdentification(new ProjectIdentification(999), 5);
        var httpContext = new DefaultHttpContext();
        httpContext.Items["ProjectId"] = CurrentProjectId.Value;
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query, new QueryCollection(new Dictionary<string, StringValues> { ["characterId"] = id.ToString() }), CultureInfo.InvariantCulture);
        var modelMetadata = MetadataProvider.GetMetadataForType(typeof(CharacterIdentification));
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext, valueProvider, modelMetadata, bindingInfo: null, modelName: "characterId");

        // Если бы allowAnotherProject был потерян (например, всегда false из-за parameterless ctor),
        // этот кейс упал бы с ошибкой "относится к другому проекту".
        await binder.BindModelAsync(bindingContext);

        bindingContext.Result.IsModelSet.ShouldBeTrue();
        bindingContext.Result.Model.ShouldBe(id);
    }

    // Регрессия: провайдер выбирался по одному лишь типу параметра, независимо от источника
    // привязки. Из-за этого он перехватывал и [FromBody]-параметры, пытаясь прочитать значение
    // из query/route/form (где его нет), и биндинг всегда возвращал null — баг в
    // ProjectFieldOperationsController.Delete, где fieldId приходил из тела запроса.
    [Fact]
    public void Provider_ForFromBodyParameter_ReturnsNull_SoDefaultBodyBinderIsUsed()
    {
        var method = typeof(ProviderTestActions).GetMethod(nameof(ProviderTestActions.WithFromBody))!;
        var parameterInfo = method.GetParameters().Single(p => p.ParameterType == typeof(CharacterIdentification));
        var modelMetadata = MetadataProvider.GetMetadataForParameter(parameterInfo);
        var providerContext = new TestModelBinderProviderContext(modelMetadata, MetadataProvider);

        var binder = Provider.GetBinder(providerContext);

        binder.ShouldBeNull();
    }

    private static class ProviderTestActions
    {
        public static void WithoutAttribute(CharacterIdentification characterId) { }

        public static void WithAttribute([AllowAnotherProject] CharacterIdentification characterId) { }

        public static void WithFromBody([FromBody] CharacterIdentification characterId) { }
    }

    private sealed class TestModelBinderProviderContext(ModelMetadata metadata, IModelMetadataProvider metadataProvider) : ModelBinderProviderContext
    {
        public override BindingInfo BindingInfo { get; } = new();
        public override ModelMetadata Metadata { get; } = metadata;
        public override IModelMetadataProvider MetadataProvider { get; } = metadataProvider;

        public override IModelBinder CreateBinder(ModelMetadata metadata) => throw new NotSupportedException();
    }
}
