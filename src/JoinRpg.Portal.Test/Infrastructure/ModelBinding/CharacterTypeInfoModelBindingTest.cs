using System.Globalization;
using JoinRpg.DomainTypes.Characters;
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
/// Форма персонажа постит <see cref="CharacterTypeInfo"/>, и MVC собирает его через конструктор.
/// Здесь закреплено, что флаг «игрок может подать заявку» (#3303) доходит до модели и как ведёт
/// себя биндинг, когда поле не пришло или форма постит поля, которые для выбранного типа не нужны.
/// </summary>
public class CharacterTypeInfoModelBindingTest
{
    private static readonly IServiceProvider Services =
        new ServiceCollection()
            .AddLogging()
            .AddMvc(options => options.ModelBinderProviders.Insert(0, new CharacterTypeInfoModelBinderProvider()))
            .Services.BuildServiceProvider();

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

    // В интерактивной форме «Горячая роль» при снятой галочке скрыта и не постится. В пререндере (клик до
    // загрузки WASM) она остаётся отмеченной — форма должна сохраниться так же, как из острова.
    [Fact]
    public async Task HotAndClosed_DropsHot()
    {
        var (result, modelState) = await BindAsync(Form(
            ("CharacterType", "Player"), ("CharacterVisibility", "Public"), ("IsHot", "true"), ("IsAcceptingClaims", "false")));

        modelState.ErrorCount.ShouldBe(0);
        var model = (CharacterTypeInfo)result.Model!;
        model.IsHot.ShouldBeFalse();
        model.IsAcceptingClaims.ShouldBeFalse();
    }

    [Fact]
    public async Task HotNpc_DropsHot()
    {
        var (result, modelState) = await BindAsync(Form(
            ("CharacterType", "NonPlayer"), ("CharacterVisibility", "Public"), ("IsHot", "true")));

        modelState.ErrorCount.ShouldBe(0);
        ((CharacterTypeInfo)result.Model!).IsHot.ShouldBeFalse();
    }

    [Fact]
    public async Task HotAndOpen_KeepsHot()
    {
        var (result, modelState) = await BindAsync(Form(
            ("CharacterType", "Player"), ("CharacterVisibility", "Public"), ("IsHot", "true"), ("IsAcceptingClaims", "true")));

        modelState.ErrorCount.ShouldBe(0);
        ((CharacterTypeInfo)result.Model!).IsHot.ShouldBeTrue();
    }

    [Fact]
    public async Task HotAndClosedSlot_DropsHot()
    {
        var (result, modelState) = await BindAsync(Form(
            ("CharacterType", "Slot"), ("CharacterVisibility", "Public"), ("IsHot", "true"), ("IsAcceptingClaims", "false")));

        modelState.ErrorCount.ShouldBe(0);
        var model = (CharacterTypeInfo)result.Model!;
        model.IsHot.ShouldBeFalse();
        model.IsAcceptingClaims.ShouldBeFalse();
    }

    // Шаблон переключили на игрока или NPC до загрузки WASM — поля шаблона остались в форме.
    [Theory]
    [InlineData("Player")]
    [InlineData("NonPlayer")]
    public async Task SlotFieldsOfNotSlot_AreDropped(string characterType)
    {
        var (result, modelState) = await BindAsync(Form(
            ("CharacterType", characterType), ("CharacterVisibility", "Public"), ("SlotLimit", "5"), ("SlotName", "Стражник")));

        modelState.ErrorCount.ShouldBe(0);
        var model = (CharacterTypeInfo)result.Model!;
        model.SlotLimit.ShouldBeNull();
        model.SlotName.ShouldBeNull();
    }

    [Fact]
    public async Task SlotFieldsOfSlot_AreKept()
    {
        var (result, modelState) = await BindAsync(Form(
            ("CharacterType", "Slot"), ("CharacterVisibility", "Public"), ("SlotLimit", "5"), ("SlotName", "Стражник")));

        modelState.ErrorCount.ShouldBe(0);
        var model = (CharacterTypeInfo)result.Model!;
        model.SlotLimit.ShouldBe(5);
        model.SlotName.ShouldBe("Стражник");
    }

    [Fact]
    public async Task UnparsableField_IsModelStateError()
    {
        var (_, modelState) = await BindAsync(Form(
            ("CharacterType", "Slot"), ("CharacterVisibility", "Public"), ("SlotLimit", "много")));

        modelState["CharacterTypeInfo.SlotLimit"]!.Errors.ShouldNotBeEmpty();
    }

    // Модель не создаётся — дальше срабатывает [Required] на свойстве вью-модели.
    [Fact]
    public async Task EmptyForm_IsNotBound()
    {
        var (result, _) = await BindAsync([]);

        result.IsModelSet.ShouldBeFalse();
    }
}
