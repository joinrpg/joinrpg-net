using System.Runtime.CompilerServices;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Characters;

internal class CharacterPropsService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository metadataRepository,
    FieldSaveHelper fieldSaveHelper,
    ILogger<CharacterPropsService> logger)
    : ICharacterPropsService
{
    public Task ChangeCharacter<TArgs>(
        CharacterIdentification characterId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<CharacterMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "")
        => ChangeCharacterCore(characterId, requiredPermission, activeRequirement, arguments,
            AsFunc(action), operationName);

    public Task<TResult> ChangeCharacter<TArgs, TResult>(
        CharacterIdentification characterId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<CharacterMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "")
        => ChangeCharacterCore(characterId, requiredPermission, activeRequirement, arguments,
            action, operationName);

    private static Func<CharacterMutationContext<TArgs>, bool> AsFunc<TArgs>(Action<CharacterMutationContext<TArgs>> action)
        => ctx =>
        {
            action(ctx);
            return true;
        };

    private async Task<TResult> ChangeCharacterCore<TArgs, TResult>(
        CharacterIdentification characterId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<CharacterMutationContext<TArgs>, TResult> action,
        string operationName)
    {
        using var activity = CharacterPropsServiceActivity.ActivitySource.StartActivity(operationName);
        // Время фиксируем на операцию, а не на сервис: DbServiceImplBase брал его в конструкторе,
        // из-за чего вложенные операции штамповали время создания сервиса (см. ADR014).
        var now = DateTime.UtcNow;
        try
        {
            // Write-репозиторий берём из UnitOfWork: он обязан использовать тот же DbContext, через
            // который мы потом сохраняем (ADR009).
            var handle = await unitOfWork.GetCharacterAggregateWriteRepository()
                .LoadCharacterForUpdate(characterId, currentUserAccessor.UserIdentification);

            // Admin-bypass'а здесь нет — в отличие от ProjectPropsService. См. ADR014.
            _ = handle.ProjectInfo.RequestMasterAccess(currentUserAccessor, requiredPermission);

            EnsureActiveIfRequired(handle.ProjectInfo, activeRequirement);

            var ctx = new CharacterMutationContext<TArgs>(
                handle.Character, handle.CharacterInfo, handle.ProjectInfo, now, currentUserAccessor,
                handle.Add, handle.Remove, fieldSaveHelper, arguments);

            var result = action(ctx);

            await unitOfWork.SaveChangesAsync();

            await PrimeCacheIfMetadataChanged(ctx, handle.RefreshProjectInfo);

            logger.LogInformation(
                "Изменён персонаж {characterId}: операция {operation}, аргументы {@arguments}",
                characterId,
                operationName,
                arguments);

            return result;
        }
        catch (Exception e)
        {
            logger.LogWarning(
                e,
                "Не удалось изменить персонажа {characterId}: операция {operation}, аргументы {@arguments}",
                characterId,
                operationName,
                arguments);
            throw;
        }
    }

    public async Task<Character> CreateCharacter<TArgs>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<CharacterCreationContext<TArgs>, Character> factory,
        [CallerMemberName] string operationName = "")
    {
        using var activity = CharacterPropsServiceActivity.ActivitySource.StartActivity(operationName);
        var now = DateTime.UtcNow;
        try
        {
            // Персонажа ещё нет, поэтому агрегат грузить не из чего — берём проект тем же способом,
            // что и ProjectPropsService, и из того же UnitOfWork.
            var handle = await unitOfWork.GetProjectMetadataWriteRepository().LoadProjectForUpdate(projectId);

            _ = handle.ProjectInfo.RequestMasterAccess(currentUserAccessor, requiredPermission);

            EnsureActiveIfRequired(handle.ProjectInfo, activeRequirement);

            var ctx = new CharacterCreationContext<TArgs>(
                handle.Project, handle.ProjectInfo, now, currentUserAccessor, fieldSaveHelper, arguments);

            var character = factory(ctx);
            handle.Project.Characters.Add(character);

            await unitOfWork.SaveChangesAsync();

            await PrimeCacheIfMetadataChanged(ctx, handle.Refresh);

            logger.LogInformation(
                "Создан персонаж {characterId}: операция {operation}, аргументы {@arguments}",
                character.CharacterId,
                operationName,
                arguments);

            return character;
        }
        catch (Exception e)
        {
            logger.LogWarning(
                e,
                "Не удалось создать персонажа в проекте {projectId}: операция {operation}, аргументы {@arguments}",
                projectId,
                operationName,
                arguments);
            throw;
        }
    }

    private static void EnsureActiveIfRequired(ProjectInfo projectInfo, ProjectActiveRequirement activeRequirement)
    {
        if (activeRequirement == ProjectActiveRequirement.MustBeActive)
        {
            _ = projectInfo.EnsureProjectActive();
        }
    }

    /// <summary>
    /// Пересобирает <see cref="ProjectInfo"/> и обновляет кэш, но только если операция тронула
    /// метаданные. В отличие от <c>ProjectPropsService</c>, где пересборка безусловна: там каждая
    /// операция по определению меняет метаданные, здесь — меньшинство, и безусловная пересборка
    /// добавляла бы тяжёлый запрос к каждому сохранению полей.
    /// </summary>
    private async Task PrimeCacheIfMetadataChanged(
        CharacterOperationContext ctx,
        Func<Task<ProjectInfo>> refresh)
    {
        if (ctx.ProjectMetadataChanged)
        {
            metadataRepository.PrimeCache(await refresh());
        }
    }
}
