using System.Runtime.CompilerServices;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters.Claims;
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
    /// <summary>
    /// Вложенная мутация запрещена: автоприём заявки раньше входил в утверждение из середины
    /// подачи, на том же экземпляре сервиса (ADR014, §7).
    /// </summary>
    private readonly NonReentrantOperation guard = new(nameof(CharacterPropsService));

    /// <summary>
    /// Общий каркас любой операции сервиса: трассировка, защита от вложенной мутации, единое
    /// время операции и логирование успеха/неудачи. Тело (<paramref name="body"/>) получает это
    /// время и делает всё специфичное для операции — загрузку хэндла, проверку прав, сохранение.
    /// Логирование остаётся у вызывающего метода: сообщения и их параметры у каждой точки входа
    /// свои. <paramref name="logSuccess"/> получает результат, потому что созданные сущности
    /// узнают свой идентификатор только после сохранения.
    /// </summary>
    private async Task<TResult> RunOperation<TResult>(
        string operationName,
        Func<DateTime, Task<TResult>> body,
        Action<TResult> logSuccess,
        Action<Exception> logFailure)
    {
        using var activity = CharacterPropsServiceActivity.ActivitySource.StartActivity(operationName);
        using var mutation = guard.Enter(operationName);
        // Время фиксируем на операцию, а не на сервис: DbServiceImplBase брал его в конструкторе,
        // из-за чего вложенные операции штамповали время создания сервиса (см. ADR014).
        var now = DateTime.UtcNow;
        try
        {
            var result = await body(now);

            logSuccess(result);

            return result;
        }
        catch (Exception e)
        {
            logFailure(e);
            throw;
        }
    }

    public Task ChangeCharacter<TArgs>(
        CharacterIdentification characterId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<CharacterMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "")
        => RunOperation(
            operationName,
            async now =>
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

                action(ctx);

                if (!ctx.IsNoOp)
                {
                    // Отметку аудита ставит сам сервис: любая операция здесь по определению меняет
                    // персонажа, и забыть её было бы слишком легко. Для прочих задетых сущностей —
                    // второй персонаж при переносе, группы — остаётся явный ctx.MarkChanged.
                    EntityAudit.MarkChanged(handle.Character, now, currentUserAccessor.UserId);

                    await unitOfWork.SaveChangesAsync();

                    await PrimeCacheIfMetadataChanged(ctx, handle.RefreshProjectInfo);
                }

                // Результата у операции над персонажем нет, но общий каркас RunOperation
                // параметризован им — возвращаем заглушку.
                return true;
            },
            _ => logger.LogInformation(
                "Изменён персонаж {characterId}: операция {operation}, аргументы {@arguments}",
                characterId,
                operationName,
                arguments),
            e => logger.LogWarning(
                e,
                "Не удалось изменить персонажа {characterId}: операция {operation}, аргументы {@arguments}",
                characterId,
                operationName,
                arguments));

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

    private Task<TResult> ChangeClaimCore<TArgs, TResult>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimMutationContext<TArgs>, Task<TResult>> action,
        string operationName)
        => RunOperation(
            operationName,
            async now =>
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

                if (!ctx.IsNoOp)
                {
                    await unitOfWork.SaveChangesAsync();

                    await PrimeCacheIfMetadataChanged(ctx, handle.RefreshProjectInfo);

                    // Уведомления уходят строго после сохранения: до него CommentId ещё не существует.
                    foreach (var pending in ctx.PendingComments)
                    {
                        await claimNotificationService.SendNotification(
                            pending.Notification.WithCommentId(pending.Comment.CommentId));
                    }

                    // Легаси-канал — после уведомлений, как это было до миграции.
                    foreach (var send in ctx.LegacyEmails)
                    {
                        await send(emailService);
                    }
                }

                return result;
            },
            _ => logger.LogInformation(
                "Изменена заявка {claimId}: операция {operation}, аргументы {@arguments}",
                claimId,
                operationName,
                arguments),
            e => logger.LogWarning(
                e,
                "Не удалось изменить заявку {claimId}: операция {operation}, аргументы {@arguments}",
                claimId,
                operationName,
                arguments));

    public Task<Character> CreateCharacter<TArgs>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<CharacterCreationContext<TArgs>, Character> factory,
        [CallerMemberName] string operationName = "")
        => RunOperation(
            operationName,
            async now =>
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

                return character;
            },
            character => logger.LogInformation(
                "Создан персонаж {characterId}: операция {operation}, аргументы {@arguments}",
                character.CharacterId,
                operationName,
                arguments),
            e => logger.LogWarning(
                e,
                "Не удалось создать персонажа в проекте {projectId}: операция {operation}, аргументы {@arguments}",
                projectId,
                operationName,
                arguments));

    public Task<Claim> CreateClaim<TArgs>(
        CharacterIdentification characterId,
        UserIdentification playerId,
        ClaimOperation operation,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimCreationContext<TArgs>, Claim> factory,
        [CallerMemberName] string operationName = "")
        => CreateClaimCore(characterId, playerId, operation, accessRequirement, activeRequirement, arguments,
            ctx => Task.FromResult(factory(ctx)), operationName);

    public Task<Claim> CreateClaimAsync<TArgs>(
        CharacterIdentification characterId,
        UserIdentification playerId,
        ClaimOperation operation,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimCreationContext<TArgs>, Task<Claim>> factory,
        [CallerMemberName] string operationName = "")
        => CreateClaimCore(characterId, playerId, operation, accessRequirement, activeRequirement, arguments,
            factory, operationName);

    private Task<Claim> CreateClaimCore<TArgs>(
        CharacterIdentification characterId,
        UserIdentification playerId,
        ClaimOperation operation,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimCreationContext<TArgs>, Task<Claim>> factory,
        string operationName)
        => RunOperation(
            operationName,
            async now =>
            {
                var handle = await unitOfWork.GetCharacterAggregateWriteRepository()
                    .LoadCharacterForUpdate(characterId, currentUserAccessor.UserIdentification);

                // Какие права нужны — решает вызывающая операция, сервис только проверяет.
                ClaimAccess.RequestForCreation(handle.ProjectInfo, currentUserAccessor, accessRequirement);

                EnsureActiveIfRequired(handle.ProjectInfo, activeRequirement);

                // Правила считаются для игрока, на которого оформляется заявка, а не для того, кто
                // выполняет операцию.
                var player = await unitOfWork.GetUsersRepository().GetRequiredUserInfo(playerId);

                if (operation.ValidatesClaimTarget())
                {
                    // CharacterInfo уже в хэндле — отдельного запроса за агрегатом больше нет.
                    ClaimValidator.EnsureCanAddClaim(handle.CharacterInfo, player, operation);
                }

                var ctx = new ClaimCreationContext<TArgs>(
                    handle.Character, handle.CharacterInfo, handle.ProjectInfo, now, currentUserAccessor,
#pragma warning disable CS0618 // Initiator нужен только легаси-каналу писем
                    handle.Initiator,
#pragma warning restore CS0618
                    player, handle.Add, handle.LoadOtherClaim, fieldSaveHelper, arguments);

                var claim = await factory(ctx);
                handle.Add(claim);

                // Фаза 1: заявка и её дискуссия получают настоящие идентификаторы.
                await unitOfWork.SaveChangesAsync();

                // Только теперь у дискуссии есть CommentDiscussionId — до сохранения он был -1.
                var pendingComments = ctx.DeferredComments
                    .Select(deferred =>
                    {
                        var (comment, notification) = commentHelper.CreateClaimCommentWithNotification(
                            deferred.CommentText,
                            // Комментарий может относиться и к соседней заявке, которую мутировала та же
                            // операция, — так вторая роль комментирует ещё и старую заявку.
                            deferred.TargetClaim ?? claim,
                            handle.ProjectInfo,
                            deferred.ExtraAction,
                            deferred.OperationType,
                            now);
                        var pending = new PendingComment(comment, notification);
                        foreach (var decorator in deferred.Decorators)
                        {
                            _ = pending.Decorate(decorator);
                        }
                        return pending;
                    })
                    .ToList();

                // Фаза 2: сохраняются комментарии и отметки времени, которые они проставили заявке.
                await unitOfWork.SaveChangesAsync();

                await PrimeCacheIfMetadataChanged(ctx, handle.RefreshProjectInfo);

                foreach (var pending in pendingComments)
                {
                    await claimNotificationService.SendNotification(
                        pending.Notification.WithCommentId(pending.Comment.CommentId));
                }

                return claim;
            },
            claim => logger.LogInformation(
                "Создана заявка {claimId} на персонажа {characterId}: операция {operation}, аргументы {@arguments}",
                claim.ClaimId,
                characterId,
                operationName,
                arguments),
            e => logger.LogWarning(
                e,
                "Не удалось создать заявку на персонажа {characterId}: операция {operation}, аргументы {@arguments}",
                characterId,
                operationName,
                arguments));

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
