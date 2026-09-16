using System.Runtime.CompilerServices;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Characters;

internal class CharacterPropsService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository metadataRepository,
    FieldSaveHelper fieldSaveHelper,
    CommentHelper commentHelper,
    IClaimNotificationService claimNotificationService,
    IEmailService emailService,
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
            action.AsAlwaysTrueFunc(), operationName);

    public Task<TResult> ChangeCharacter<TArgs, TResult>(
        CharacterIdentification characterId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<CharacterMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "")
        => ChangeCharacterCore(characterId, requiredPermission, activeRequirement, arguments,
            action, operationName);

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
                handle, fieldSaveHelper, arguments);

            var result = action(ctx);

            // Отметку аудита ставит сам сервис: любая операция здесь по определению меняет
            // персонажа, и забыть её было бы слишком легко. Для прочих задетых сущностей —
            // второй персонаж при переносе, группы — остаётся явный ctx.MarkChanged.
            EntityAudit.MarkChanged(handle.Character, now, currentUserAccessor.UserId);

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

    public Task ChangeClaim<TArgs>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<ClaimMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "")
        => ChangeClaimCore(claimId, accessRequirement, activeRequirement, arguments,
            action.AsAlwaysTrueAsyncFunc(), operationName);

    public Task<TResult> ChangeClaim<TArgs, TResult>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "")
        => ChangeClaimCore(claimId, accessRequirement, activeRequirement, arguments,
            ctx => Task.FromResult(action(ctx)), operationName);

    public Task ChangeClaimAsync<TArgs>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimMutationContext<TArgs>, Task> action,
        [CallerMemberName] string operationName = "")
        => ChangeClaimCore(claimId, accessRequirement, activeRequirement, arguments,
            action.AsAlwaysTrueAsyncFunc(), operationName);

    public Task<TResult> ChangeClaimAsync<TArgs, TResult>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimMutationContext<TArgs>, Task<TResult>> action,
        [CallerMemberName] string operationName = "")
        => ChangeClaimCore(claimId, accessRequirement, activeRequirement, arguments,
            action, operationName);

    private async Task<TResult> ChangeClaimCore<TArgs, TResult>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimMutationContext<TArgs>, Task<TResult>> action,
        string operationName)
    {
        using var activity = CharacterPropsServiceActivity.ActivitySource.StartActivity(operationName);
        var now = DateTime.UtcNow;
        try
        {
            var handle = await unitOfWork.GetCharacterAggregateWriteRepository()
                .LoadClaimForUpdate(claimId, currentUserAccessor.UserIdentification);

            ClaimAccess.Request(handle.ProjectInfo, handle.ClaimInfo, currentUserAccessor, accessRequirement);

            EnsureActiveIfRequired(handle.ProjectInfo, activeRequirement);

            var ctx = new ClaimMutationContext<TArgs>(
                handle.Claim, handle.ClaimInfo, handle.Character, handle.CharacterInfo, handle.ProjectInfo,
                now, currentUserAccessor, handle.Initiator, handle, fieldSaveHelper,
                commentHelper, arguments);

            var result = await action(ctx);

            await unitOfWork.SaveChangesAsync();

            await PrimeCacheIfMetadataChanged(ctx, handle.RefreshProjectInfo);

            // Уведомления уходят строго после сохранения: до него CommentId ещё не существует.
            foreach (var pending in ctx.PendingComments)
            {
                if (!pending.IsSilent)
                {
                    await claimNotificationService.SendNotification(
                        pending.Notification.WithCommentId(pending.Comment.CommentId));
                }
            }

            // Легаси-канал — после уведомлений, как это было до миграции.
            foreach (var send in ctx.LegacyEmails)
            {
                await send(emailService);
            }

            logger.LogInformation(
                "Изменена заявка {claimId}: операция {operation}, аргументы {@arguments}",
                claimId,
                operationName,
                arguments);

            return result;
        }
        catch (Exception e)
        {
            logger.LogWarning(
                e,
                "Не удалось изменить заявку {claimId}: операция {operation}, аргументы {@arguments}",
                claimId,
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

            // Как и в ChangeCharacter, отметку аудита ставит сервис, а не фабрика.
            EntityAudit.MarkCreated(character, now, currentUserAccessor.UserId);

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
