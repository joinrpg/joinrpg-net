using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.CharacterFields;

/// <summary>
/// Точка входа сервисного слоя для сохранения полей персонажа или заявки: сохраняет значения
/// через <see cref="FieldSaveHelper"/> и доводит до метаданных проекта побочный эффект этого
/// сохранения — отметку впервые заполненных полей как использованных.
/// </summary>
/// <remarks>
/// Отметка живёт здесь, а не в <see cref="FieldSaveHelper"/>, потому что это изменение метаданных
/// проекта, а такие изменения идут только через <see cref="IProjectPropsService"/> (ADR009) —
/// сервисный тип, недоступный из <c>JoinRpg.Domain</c>.
/// </remarks>
internal class CharacterFieldsSaveService(
    FieldSaveHelper fieldSaveHelper,
    IProjectPropsService projectPropsService)
{
    /// <summary>
    /// Сохраняет поля заявки.
    /// </summary>
    /// <returns>Изменившиеся поля.</returns>
    /// <param name="fieldsToSet">
    /// Поля, которые надо изменить, — дельта, а не полный слой. Пустой слой — не менять ничего.
    /// </param>
    public async Task<IReadOnlyCollection<FieldWithPreviousAndNewValue>> SaveCharacterFields(
        int currentUserId,
        Claim claim,
        FieldLayerContainer fieldsToSet,
        ProjectInfo projectInfo)
    {
        var updatedFields = fieldSaveHelper.SaveCharacterFields(currentUserId, claim, fieldsToSet, projectInfo);
        await MarkAsUsed(updatedFields, projectInfo);
        return updatedFields;
    }

    /// <summary>
    /// Сохраняет поля персонажа.
    /// </summary>
    /// <returns>Изменившиеся поля.</returns>
    /// <param name="fieldsToSet">
    /// Поля, которые надо изменить, — дельта, а не полный слой. Пустой слой — не менять ничего.
    /// </param>
    public async Task<IReadOnlyCollection<FieldWithPreviousAndNewValue>> SaveCharacterFields(
        int currentUserId,
        Character character,
        FieldLayerContainer fieldsToSet,
        ProjectInfo projectInfo)
    {
        var updatedFields = fieldSaveHelper.SaveCharacterFields(currentUserId, character, fieldsToSet, projectInfo);
        await MarkAsUsed(updatedFields, projectInfo);
        return updatedFields;
    }

    /// <summary>
    /// Поля и варианты, которые надо впервые отметить как использованные. Логируются
    /// <see cref="IProjectPropsService"/> вместе с именем операции.
    /// </summary>
    private sealed record FieldsToMarkAsUsed(
        IReadOnlyCollection<int> FieldIds,
        IReadOnlyCollection<int> VariantIds)
    {
        public bool IsEmpty => FieldIds.Count == 0 && VariantIds.Count == 0;
    }

    /// <summary>
    /// Отмечает заполненные поля и выбранные варианты как использованные
    /// (<see cref="ProjectField.WasEverUsed"/>). Флаг входит в <see cref="ProjectInfo"/> и
    /// запрещает окончательное удаление поля, то есть это изменение метаданных проекта.
    /// </summary>
    private async Task MarkAsUsed(
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields,
        ProjectInfo projectInfo)
    {
        var toMark = CollectNotYetMarked(updatedFields);
        if (toMark.IsEmpty)
        {
            // Обычный случай: всё давно отмечено. В БД не идём вовсе — иначе на самом горячем
            // пути записи получили бы лишнюю загрузку проекта и лишний SaveChanges.
            return;
        }

        await projectPropsService.ChangeProjectPropertiesAsSideEffect(
            projectInfo.ProjectId,
            // Отметка — бухгалтерия, а не намерение пользователя. Уронить из-за неё сохранение
            // заявки на закрытом проекте было бы хуже, чем отметить поле в закрытом проекте.
            ProjectActiveRequirement.AllowInactive,
            toMark,
            ctx =>
            {
                foreach (var field in ctx.Project.ProjectFields)
                {
                    if (ctx.Request.FieldIds.Contains(field.ProjectFieldId))
                    {
                        field.WasEverUsed = true;
                    }

                    foreach (var variant in field.DropdownValues)
                    {
                        if (ctx.Request.VariantIds.Contains(variant.ProjectFieldDropdownValueId))
                        {
                            variant.WasEverUsed = true;
                        }
                    }
                }
            },
            operationName: nameof(MarkAsUsed));
    }

    /// <summary>
    /// Отбирает из изменившихся полей те, что ещё не отмечены. Отметка монотонна (только
    /// <c>false → true</c>), поэтому по снимку метаданных видно, есть ли вообще что менять, —
    /// без обращения к БД.
    /// </summary>
    private static FieldsToMarkAsUsed CollectNotYetMarked(
        IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields)
    {
        var fieldIds = new HashSet<int>();
        var variantIds = new HashSet<int>();

        foreach (var updated in updatedFields)
        {
            var field = updated.New.Field;

            if (!field.WasEverUsed)
            {
                _ = fieldIds.Add(field.Id.ProjectFieldId);
            }

            if (field.HasValueList)
            {
                foreach (var variant in updated.New.GetDropdownValues().Where(v => !v.WasEverUsed))
                {
                    _ = variantIds.Add(variant.Id.ProjectFieldVariantId);
                }
            }
        }

        return new FieldsToMarkAsUsed(fieldIds, variantIds);
    }
}
