using JoinRpg.DomainTypes.Characters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JoinRpg.Portal.Infrastructure.ModelBinding;

/// <summary>
/// Поля формы персонажа, как их постит <c>CharacterTypeSelector</c>. Имена параметров совпадают
/// с конструктором <see cref="CharacterTypeInfo"/> — разметка формы та же.
/// </summary>
public sealed record CharacterTypeInfoForm(
    CharacterType CharacterType,
    bool IsHot,
    int? SlotLimit,
    string? SlotName,
    CharacterVisibility CharacterVisibility,
    bool IsAcceptingClaims = true)
{
    /// <summary>
    /// Отбрасывает поля, которые интерактивный остров для выбранного типа не показывает и не постит:
    /// «Горячую роль» у NPC и у закрытой для игроков роли, лимит и название у не-шаблона.
    /// В пререндере (до загрузки WASM) остров не реагирует на клики, и форма постит их как есть —
    /// без отбрасывания инвариант <see cref="CharacterTypeInfo"/> молча не давал сохранить форму.
    /// </summary>
    public CharacterTypeInfo ToCharacterTypeInfo()
    {
        var isSlot = CharacterType == CharacterType.Slot;
        var canBeHot = IsAcceptingClaims && CharacterType != CharacterType.NonPlayer;
        return new CharacterTypeInfo(
            CharacterType,
            IsHot && canBeHot,
            isSlot ? SlotLimit : null,
            isSlot ? SlotName : null,
            CharacterVisibility,
            IsAcceptingClaims);
    }
}

/// <summary>
/// Биндит <see cref="CharacterTypeInfo"/> через <see cref="CharacterTypeInfoForm"/>, см. там.
/// </summary>
public sealed class CharacterTypeInfoModelBinder(IModelBinder formBinder, ModelMetadata formMetadata) : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ModelBindingResult formResult;
        using (bindingContext.EnterNestedScope(formMetadata, bindingContext.FieldName, bindingContext.ModelName, model: null))
        {
            await formBinder.BindModelAsync(bindingContext);
            formResult = bindingContext.Result;
        }

        // После отбрасывания полей инвариантам CharacterTypeInfo нарушаться нечем.
        bindingContext.Result = formResult.Model is CharacterTypeInfoForm form
            ? ModelBindingResult.Success(form.ToCharacterTypeInfo())
            : ModelBindingResult.Failed();
    }
}

public sealed class CharacterTypeInfoModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType != typeof(CharacterTypeInfo))
        {
            return null;
        }

        var formMetadata = context.MetadataProvider.GetMetadataForType(typeof(CharacterTypeInfoForm));
        return new CharacterTypeInfoModelBinder(context.CreateBinder(formMetadata), formMetadata);
    }
}
