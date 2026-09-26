using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Characters;

namespace JoinRpg.Services.Impl;

internal class CharacterServiceImpl(ICharacterPropsService characterPropsService) : ICharacterService
{
    public async Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest)
    {
        var character = await characterPropsService.CreateCharacter(
            addCharacterRequest.ProjectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            addCharacterRequest,
            ctx =>
            {
                var character = new Character
                {
                    // Здесь — только обычные группы, выбранные мастером; спецгруппы допишет
                    // SaveFields ниже (см. подробный комментарий в EditCharacter).
                    ParentCharacterGroupIds = ctx.ProjectInfo.GroupTree
                        .ValidateGroupListForCharacter(ctx.Request.ParentCharacterGroupIds).ToIntArray(),
                    ProjectId = ctx.Request.ProjectId,
                    Project = ctx.Project,
                };

                ctx.SetCharacterSettings(character, ctx.Request.CharacterTypeInfo);

                //TODO we do not send message for creating character
                _ = ctx.SaveFields(character, ctx.Request.FieldValues);

                return character;
            });

        // CharacterId генерируется БД при сохранении — читаем уже после возврата из сервиса.
        return new CharacterIdentification(character.ProjectId, character.CharacterId);
    }

    public Task EditCharacter(EditCharacterRequest editCharacterRequest)
        => characterPropsService.ChangeCharacter(
            editCharacterRequest.Id,
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            editCharacterRequest,
            ctx =>
            {
                ctx.SetCharacterSettings(ctx.Request.CharacterTypeInfo);

                // Присваивание записывает ТОЛЬКО обычные группы, выбранные мастером: спецгруппы,
                // которые были у персонажа, из сущности при этом исчезают. Возвращает их обратно
                // SaveFields: CharacterExistsStrategyBase.ComputeParentGroupIds объединяет
                // спецгруппы, выведенные из значений полей, с обычными группами, которые он
                // вычитывает с персонажа, а FieldSaveHelper.Apply пишет результат в
                // ParentCharacterGroupIds. То есть это не полная перезапись списка групп, а
                // сознательно «половина» двухшагового рукопожатия — половина обычных групп.
                // Между двумя шагами спецгрупп у персонажа нет, и доступность полей считается
                // именно в этот момент (поведение унаследованное, вынесено в отдельную задачу).
                ctx.Character.ParentCharacterGroupIds = ctx.ProjectInfo.GroupTree
                    .ValidateGroupListForCharacter(ctx.Request.ParentCharacterGroupIds).ToIntArray();

                _ = ctx.SaveFields(ctx.Request.FieldValues);

                // TODO: восстановить отправку письма об изменении полей, см. ADR014.
            });

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

                if (ctx.Character.Project.Details.DefaultTemplateCharacter == ctx.Character)
                {
                    throw new DefaultTemplateCharacterCannotBeDeletedException(ctx.Request.Id);
                }

                // Связи с сюжетами не рвём: удаление мягкое (IsActive = false), персонажа можно
                // вернуть. Раньше здесь стояла очистка под Character.CanBePermanentlyDeleted —
                // ветка была мертва, потому что это public-поле со значением false, которое ничему
                // другому не присваивается и в БД не отображается.
                ctx.Character.IsActive = false;
            });

    public Task SetFields(CharacterIdentification characterId, FieldLayerContainer fieldsToSet)
        => characterPropsService.ChangeCharacter(
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
