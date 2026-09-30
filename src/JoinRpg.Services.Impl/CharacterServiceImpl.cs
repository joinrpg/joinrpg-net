using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Characters;

namespace JoinRpg.Services.Impl;

internal class CharacterServiceImpl(
    ICharacterPropsService characterPropsService,
    UserFieldValidator userFieldValidator) : ICharacterService
{
    public async Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest)
    {
        // Существование упомянутых пользователей проверяется до сохранения: FieldSaveHelper
        // синхронный и репозиториев не видит (ADR017 §7).
        await userFieldValidator.ValidateUserFields(addCharacterRequest.FieldValues);

        var character = await characterPropsService.CreateCharacter(
            addCharacterRequest.ProjectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            addCharacterRequest,
            ctx =>
            {
                // Группы здесь не проставляются: итоговый список (выбранные мастером обычные плюс
                // спецгруппы по значениям полей) пишет SaveFields ниже.
                var character = new Character
                {
                    ProjectId = ctx.Request.ProjectId,
                    Project = ctx.Project,
                };

                ctx.SetCharacterSettings(character, ctx.Request.CharacterTypeInfo);

                //TODO we do not send message for creating character
                _ = ctx.SaveFields(
                    character,
                    ctx.Request.FieldValues,
                    ctx.ProjectInfo.GroupTree.ValidateGroupListForCharacter(ctx.Request.ParentCharacterGroupIds));

                return character;
            });

        // CharacterId генерируется БД при сохранении — читаем уже после возврата из сервиса.
        return new CharacterIdentification(character.ProjectId, character.CharacterId);
    }

    public async Task EditCharacter(EditCharacterRequest editCharacterRequest)
    {
        await userFieldValidator.ValidateUserFields(editCharacterRequest.FieldValues);

        await characterPropsService.ChangeCharacter(
            editCharacterRequest.Id,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            editCharacterRequest,
            ctx =>
            {
                ctx.SetCharacterSettings(ctx.Request.CharacterTypeInfo);

                // Выбранные мастером группы — всегда только обычные (форма и присылает лишь их,
                // см. CharacterController.EditCharacter). Спецгруппы персонаж получает
                // автоматически по значениям полей, поэтому итоговый список групп собирает и пишет
                // SaveFields: обычные берёт отсюда, спецгруппы пересчитывает сам. Сущность до
                // сохранения не трогаем — иначе внутри сохранения у персонажа не окажется
                // спецгрупп, а по ним считается доступность полей (#4937).
                _ = ctx.SaveFields(
                    ctx.Request.FieldValues,
                    ctx.ProjectInfo.GroupTree.ValidateGroupListForCharacter(ctx.Request.ParentCharacterGroupIds));

                // TODO: восстановить отправку письма об изменении полей, см. ADR014.
            });
    }

    public Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest)
        => characterPropsService.ChangeCharacter(
            deleteCharacterRequest.Id,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            deleteCharacterRequest,
            ctx =>
            {
                if (ctx.CharacterInfo.HasActiveClaims)
                {
                    throw new CharacterHasActiveClaimsException(ctx.Request.Id);
                }

                // Шаблон берём из снимка метаданных, а не через навигацию
                // Details.DefaultTemplateCharacter — та дала бы ленивую загрузку персонажа (#4987).
                if (ctx.ProjectInfo.ClaimSettings.DefaultTemplate == ctx.CharacterInfo.Id)
                {
                    throw new DefaultTemplateCharacterCannotBeDeletedException(ctx.Request.Id);
                }

                // Связи с сюжетами не рвём: удаление мягкое (IsActive = false), персонажа можно
                // вернуть. Раньше здесь стояла очистка под Character.CanBePermanentlyDeleted —
                // ветка была мертва, потому что это public-поле со значением false, которое ничему
                // другому не присваивается и в БД не отображается.
                ctx.Character.IsActive = false;
            });

    public async Task SetFields(CharacterIdentification characterId, FieldLayerContainer fieldsToSet)
    {
        await userFieldValidator.ValidateUserFields(fieldsToSet);

        await characterPropsService.ChangeCharacter(
            characterId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            fieldsToSet,
            ctx =>
            {
                _ = ctx.SaveFields(ctx.Request);

                // TODO: восстановить отправку письма об изменении полей, см. ADR014.
            });
    }
}
