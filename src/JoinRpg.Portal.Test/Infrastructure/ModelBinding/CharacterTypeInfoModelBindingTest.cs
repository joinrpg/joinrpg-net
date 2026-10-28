using System.Globalization;
using JoinRpg.DomainTypes.Characters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace JoinRpg.Portal.Test.Infrastructure.ModelBinding;

/// <summary>
/// Форма персонажа постит <see cref="CharacterTypeInfo"/>, и MVC собирает его через конструктор.
/// Здесь закреплено, что флаг «игрок может подать заявку» (#3303) доходит до модели и как ведёт
/// себя биндинг, когда поле не пришло или сочетание полей нарушает инвариант.
/// </summary>
public class CharacterTypeInfoModelBindingTest
{
    private static readonly IServiceProvider Services =
        new ServiceCollection().AddLogging().AddMvc().Services.BuildServiceProvider();

    private static async Task<(ModelBindingResult Result, ModelStateDictionary ModelState)> BindAsync(
        Dictionary<string, StringValues> form)
    {
        var httpContext = new DefaultHttpContext { RequestServices = Services };
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);
        var valueProvider = new FormValueProvider(BindingSource.Form, new FormCollection(form), CultureInfo.InvariantCulture);

        var metadataProvider = Services.GetRequiredService<IModelMetadataProvider>();
        var metadata = metadataProvider.GetMetadataForType(typeof(CharacterTypeInfo));
        var binder = Services.GetRequiredService<IModelBinderFactory>()
            .CreateBinder(new ModelBinderFactoryContext { Metadata = metadata, CacheToken = metadata });

        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext, valueProvider, metadata, bindingInfo: null, modelName: "CharacterTypeInfo");
        await binder.BindModelAsync(bindingContext);
        return (bindingContext.Result, modelState);
    }

    private static Dictionary<string, StringValues> Form(params (string Key, string Value)[] fields)
        => fields.ToDictionary(f => "CharacterTypeInfo." + f.Key, f => new StringValues(f.Value));

    [Fact]
    public async Task ClosedFlagReachesModel()
    {
        var (result, _) = await BindAsync(Form(
            ("CharacterType", "Player"), ("CharacterVisibility", "Public"), ("IsAcceptingClaims", "false")));

        result.IsModelSet.ShouldBeTrue();
        ((CharacterTypeInfo)result.Model!).IsAcceptingClaims.ShouldBeFalse();
    }

    [Fact]
    public async Task MissingFlagMeansOpen()
    {
        var (result, _) = await BindAsync(Form(("CharacterType", "Player"), ("CharacterVisibility", "Public")));

        ((CharacterTypeInfo)result.Model!).IsAcceptingClaims.ShouldBeTrue();
    }

    // В интерактивной форме «Горячая роль» при снятой галочке скрыта, но в пререндере (клик до загрузки
    // WASM) или ручным POST сочетание приходит. 500 быть не должно: исключение конструктора биндер
    // кладёт в ModelState, и форма показывается заново.
    [Fact]
    public async Task HotAndClosed_IsModelStateErrorNot500()
    {
        var (result, modelState) = await BindAsync(Form(
            ("CharacterType", "Player"), ("CharacterVisibility", "Public"), ("IsHot", "true"), ("IsAcceptingClaims", "false")));

        modelState.IsValid.ShouldBeFalse();
        result.IsModelSet.ShouldBeFalse();
    }
}
