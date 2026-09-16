using System.Data.Entity.Validation;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
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
                    ParentCharacterGroupIds = ctx.ValidateGroupListForCharacter(ctx.Request.ParentCharacterGroupIds),
                    ProjectId = ctx.Request.ProjectId,
                    Project = ctx.Project,
                };

                ctx.SetCharacterSettings(character, ctx.Request.CharacterTypeInfo);
                ctx.MarkCreatedNow(character);

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

                ctx.Character.ParentCharacterGroupIds =
                    ctx.ValidateGroupListForCharacter(ctx.Request.ParentCharacterGroupIds);

                _ = ctx.SaveFields(ctx.Request.FieldValues);

                ctx.MarkChanged(ctx.Character);

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
                if (ctx.CharacterInfo.HasActiveClaims
                    || ctx.Character.Project.Details.DefaultTemplateCharacter == ctx.Character)
                {
                    throw new DbEntityValidationException();
                }

                if (ctx.Character.CanBePermanentlyDeleted)
                {
                    ctx.Character.DirectlyRelatedPlotElements.CleanLinksList();
                }

                ctx.Character.IsActive = false;
                ctx.MarkChanged(ctx.Character);
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

                ctx.MarkChanged(ctx.Character);

                // TODO: восстановить отправку письма об изменении полей, см. ADR014.
            });
}
