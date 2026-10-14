using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Drp;

namespace JoinRpg.Services.Impl.Claims;

/// <inheritdoc cref="IExportRestoreService"/>
/// <remarks>
/// <para>
/// Отметка полей использованными (<see cref="IFieldSetupService.MarkFieldsUsedIfNotUsedYet"/>) — до
/// операции, как у обычных сохранений, но после проверок блокировки и прав: так её не поставит тот, кто
/// восстанавливать не вправе. Ложная отметка при неудаче операции безвредна.
/// </para>
/// <para>
/// Уведомлений нет: операции над персонажем их не шлют вовсе, а над заявкой история пишется мимо
/// очереди (<see cref="ClaimMutationContext.AddHistoryRecordWithoutNotification"/>).
/// </para>
/// <para>
/// «Ничего не изменилось» решается по снимку сущностей до и после сохранения, а не по списку изменённых
/// полей: сохранение может посчитать поле изменённым, не изменив записанного (и наоборот). Если снимки
/// равны, присвоения откатываются — трекаемые сущности остаются такими, какими были загружены.
/// </para>
/// </remarks>
internal class ExportRestoreService(
    ICharacterPropsService characterPropsService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    IFieldSetupService fieldSetupService,
    UserFieldValidator userFieldValidator) : IExportRestoreService
{
    public async Task ApplyRestoredCharacter(CharacterIdentification characterId, RestoredCharacterTarget target)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterId.ProjectId);
        EnsureProjectBlocked(projectInfo);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanEditRoles);

        var fields = target.Fields is null
            ? FieldLayerContainer.Empty(projectInfo)
            : new FieldLayerContainer(projectInfo, target.Fields);
        if (fields.LayerData.Keys.FirstOrDefault(id => projectInfo.GetFieldById(id).BoundTo == FieldBoundTo.Claim) is { } claimFieldId)
        {
            throw new ArgumentException($"Поле {claimFieldId} — поле заявки, а не персонажа", nameof(target));
        }

        await userFieldValidator.ValidateUserFields(fields);
        await fieldSetupService.MarkFieldsUsedIfNotUsedYet(fields);

        await characterPropsService.ChangeCharacter(
            characterId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.AllowBlocked,
            target,
            ctx =>
            {
                EnsureProjectBlocked(ctx.ProjectInfo);

                var regularGroupIds = ctx.Request.Groups is { } groups
                    ? ctx.ProjectInfo.GroupTree.ValidateGroupListForCharacter(groups)
                    : null;

                var before = CharacterSnapshot.Take(ctx.Character);

                // Группы пишет само сохранение: обычные — переданные (или текущие), спецгруппы — по
                // значениям полей, как в EditCharacter.
                _ = ctx.SaveRestoredFields(fields, regularGroupIds);

                // Отметку аудита (UpdatedAt = сейчас) ставит ChangeCharacter — восстановленный персонаж
                // поднимает дату обновления проекта (ADR023).
                if (before.SameAs(ctx.Character, ctx.ProjectInfo))
                {
                    before.RollBack(ctx.Character);
                    ctx.NothingChanged();
                }
            });
    }

    public async Task ApplyRestoredClaimFields(
        ClaimIdentification claimId,
        IReadOnlyDictionary<ProjectFieldIdentification, string?> fields)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);
        EnsureProjectBlocked(projectInfo);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims);

        var layer = new FieldLayerContainer(projectInfo, fields);
        await userFieldValidator.ValidateUserFields(layer);
        await fieldSetupService.MarkFieldsUsedIfNotUsedYet(layer);

        await characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.ManageClaims,
            ProjectActiveRequirement.AllowBlocked,
            layer,
            ctx =>
            {
                EnsureProjectBlocked(ctx.ProjectInfo);

                var claim = ctx.Claim;
                var character = claim.Character;
                var toSave = claim.IsApproved ? ctx.Request : WithoutCharacterValues(ctx.Request, character, ctx.ProjectInfo);

                var claimJsonBefore = claim.JsonData;
                var characterBefore = CharacterSnapshot.Take(character);

                var updatedFields = ctx.SaveRestoredFields(toSave);

                if (SameFields(claimJsonBefore, claim.JsonData, ctx.ProjectInfo) && characterBefore.SameAs(character, ctx.ProjectInfo))
                {
                    claim.JsonData = claimJsonBefore;
                    characterBefore.RollBack(character);
                    ctx.NothingChanged();
                    return;
                }

                // Персонаж меняется, только если заявка принята: у непринятой поля пишутся в саму заявку.
                if (claim.IsApproved && updatedFields.Any(f => f.Field.BoundTo == FieldBoundTo.Character))
                {
                    ctx.MarkChanged(character);
                }

                // Запись в истории заодно поднимает Claim.LastUpdateDateTime до текущего момента (ADR023).
                // TODO[Localize]
                _ = ctx.AddHistoryRecordWithoutNotification(
                    "Восстановлено из выгрузки после потери данных: "
                        + string.Join(", ", updatedFields.Select(f => $"«{f.Field.Name}»"))
                        + ".",
                    CommentExtraAction.RestoredFromExport);
            });
    }

    /// <summary>
    /// Для непринятой заявки: убирает поля персонажа, значение которых совпадает с полным значением у
    /// самого персонажа. Это не предложение игрока, а то, что заявка видит через персонажа, — а стратегия
    /// сохранения в непринятую заявку сравнивает только с <b>публичными</b> значениями персонажа и иначе
    /// скопировала бы в заявку непубличное (то, от чего она защищает).
    /// </summary>
    private static FieldLayerContainer WithoutCharacterValues(
        FieldLayerContainer layer,
        Character character,
        ProjectInfo projectInfo)
    {
        var characterLayer = FieldLayerContainer.DeserializeFieldLayer(projectInfo, character.JsonData);

        var kept = layer.LayerData.Values
            .Where(v => v.Field.BoundTo != FieldBoundTo.Character
                || Normalize(v.Value) != Normalize(characterLayer.GetValue(v.Field)))
            .ToDictionary(v => v.Field.Id.ProjectFieldId, v => v.Value);

        return new FieldLayerContainer(projectInfo, kept);
    }

    private static string? Normalize(string? value) => string.IsNullOrEmpty(value) ? null : value;

    /// <summary>Совпадают ли значения полей двух сериализованных слоёв (пустое значение = отсутствие).</summary>
    private static bool SameFields(string? before, string? after, ProjectInfo projectInfo)
    {
        if (before == after)
        {
            return true;
        }

        static Dictionary<ProjectFieldIdentification, string> Values(string? json, ProjectInfo projectInfo)
            => FieldLayerContainer.DeserializeFieldLayer(projectInfo, json).LayerData.Values
                .Where(v => !string.IsNullOrEmpty(v.Value))
                .ToDictionary(v => v.Field.Id, v => v.Value!);

        var a = Values(before, projectInfo);
        var b = Values(after, projectInfo);
        return a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var value) && value == kv.Value);
    }

    /// <summary>То, что сохранение полей пишет в персонажа, — исходные значения для сравнения и отката.</summary>
    private sealed record CharacterSnapshot(
        string JsonData,
        string CharacterName,
        MarkdownDbValue Description,
        int[] ParentCharacterGroupIds)
    {
        public static CharacterSnapshot Take(Character character)
            => new(character.JsonData, character.CharacterName, character.Description, character.ParentCharacterGroupIds);

        public bool SameAs(Character character, ProjectInfo projectInfo)
            => SameFields(JsonData, character.JsonData, projectInfo)
                && CharacterName == character.CharacterName
                && (Description?.Contents ?? "") == (character.Description?.Contents ?? "")
                && ParentCharacterGroupIds.ToHashSet().SetEquals(character.ParentCharacterGroupIds);

        public void RollBack(Character character)
        {
            character.JsonData = JsonData;
            character.CharacterName = CharacterName;
            character.Description = Description;
            character.ParentCharacterGroupIds = ParentCharacterGroupIds;
        }
    }

    private static void EnsureProjectBlocked(ProjectInfo projectInfo)
    {
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.Blocked)
        {
            // TODO[Localize]
            throw new JoinValidationException("Восстановление из выгрузки доступно только в заблокированном проекте.");
        }
    }
}
