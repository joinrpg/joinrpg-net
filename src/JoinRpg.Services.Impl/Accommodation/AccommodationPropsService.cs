using System.Runtime.CompilerServices;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Accommodation;

/// <summary>
/// Реализация <see cref="IAccommodationPropsService"/> (ADR018, §10).
/// </summary>
/// <remarks>
/// Кеш метаданных здесь не трогается вовсе: план поселения — оперативные данные, в
/// <c>ProjectInfo</c> они не входят (ADR015), значит ни пересборки снимка, ни <c>PrimeCache</c>
/// после сохранения не требуется.
/// </remarks>
internal class AccommodationPropsService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository metadataRepository,
    IEmailService emailService,
    ILogger<AccommodationPropsService> logger)
    : IAccommodationPropsService
{
    /// <summary>
    /// Вложенная мутация запрещена по той же причине, что в ADR009/ADR014: внешняя операция уже
    /// зафиксировала время и держит собственный снимок плана, который вложенная обесценила бы
    /// незаметно.
    /// </summary>
    private readonly NonReentrantOperation guard = new(nameof(AccommodationPropsService));

    public Task<TResult> ChangePlan<TArgs, TResult>(
        RoomCategoryIdentification categoryId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        RoomCategoryPlanTracking tracking,
        TArgs arguments,
        Func<RoomCategoryPlanMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "")
        => ChangePlanCore(
            categoryId.ProjectId,
            // Write-репозиторий берём из UnitOfWork: он обязан использовать тот же DbContext,
            // через который мы потом сохраняем (ADR009 §1, ADR014 §2).
            (write, projectInfo) => write.LoadPlanForUpdate(projectInfo, categoryId, tracking),
            categoryId.ToString(),
            requiredPermission,
            activeRequirement,
            arguments,
            action,
            operationName);

    public Task ChangePlan<TArgs>(
        RoomCategoryIdentification categoryId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        RoomCategoryPlanTracking tracking,
        TArgs arguments,
        Action<RoomCategoryPlanMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "")
        => ChangePlan(
            categoryId,
            requiredPermission,
            activeRequirement,
            tracking,
            arguments,
            action.AsAlwaysTrueFunc(),
            operationName);

    public Task ChangePlanForRoom<TArgs>(
        AccommodationRoomIdentification roomId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        RoomCategoryPlanTracking tracking,
        TArgs arguments,
        Action<RoomCategoryPlanMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "")
        => ChangePlanCore(
            roomId.ProjectId,
            (write, projectInfo) => write.LoadPlanForRoomUpdate(projectInfo, roomId, tracking),
            roomId.ToString(),
            requiredPermission,
            activeRequirement,
            arguments,
            action.AsAlwaysTrueFunc(),
            operationName);

    public Task ChangePlanForGroup<TArgs>(
        AccommodationRequestIdentification groupId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<RoomCategoryPlanMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "")
        => ChangePlanCore(
            groupId.ProjectId,
            // Параметра tracking нет: корень агрегата назван через группу — значит операция её и
            // двигает, трекаемые группы нужны всегда.
            (write, projectInfo) => write.LoadPlanForGroupUpdate(projectInfo, groupId),
            groupId.ToString(),
            requiredPermission,
            activeRequirement,
            arguments,
            action.AsAlwaysTrueFunc(),
            operationName);

    /// <summary>
    /// Общий цикл операции: загрузка хэндла, право, активность проекта, мутация, сохранение и лог.
    /// Чем именно назван корень агрегата — категорией или комнатой, — знает только
    /// <paramref name="loadHandle"/>.
    /// </summary>
    private async Task<TResult> ChangePlanCore<TArgs, TResult>(
        ProjectIdentification projectId,
        Func<IRoomCategoryPlanWriteRepository, ProjectInfo, Task<IRoomCategoryPlanUpdateHandle>> loadHandle,
        string targetId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<RoomCategoryPlanMutationContext<TArgs>, TResult> action,
        string operationName)
    {
        using var activity = AccommodationPropsServiceActivity.ActivitySource.StartActivity(operationName);
        using var mutation = guard.Enter(operationName);
        // Время фиксируем на операцию, а не на сервис (ADR014).
        var now = DateTimeOffset.UtcNow;
        try
        {
            // Снимок метаданных берём из общего репозитория: он кеширован на запрос, поэтому
            // на пути записи это, как правило, попадание в кеш, а не ещё одна загрузка проекта.
            // Своего снимка write-репозиторию заводить не нужно: план поселения его только читает
            // (права, активность проекта, ссылки на типы), трекаемый Project ему не нужен.
            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);

            var handle = await loadHandle(unitOfWork.GetRoomCategoryPlanWriteRepository(), projectInfo);

            // Админ (в т.ч. робот, под которым выполняются фоновые джобы) проходит проверку прав —
            // как в ProjectPropsService: поселение целиком мастерский контур, игрокам сюда нельзя.
            if (!currentUserAccessor.IsAdmin)
            {
                _ = handle.ProjectInfo.RequestMasterAccess(currentUserAccessor, requiredPermission);
            }

            if (activeRequirement == ProjectActiveRequirement.MustBeActive)
            {
                _ = handle.ProjectInfo.EnsureProjectActive();
            }

            var ctx = new RoomCategoryPlanMutationContext<TArgs>(
                handle.Plan, handle, now, currentUserAccessor, arguments);

            var result = action(ctx);

            await unitOfWork.SaveChangesAsync();

            // Письма легаси-канала уходят строго ПОСЛЕ успешного сохранения (ADR018, §11):
            // до него операция ещё может упасть, и рассылка оказалась бы ложной.
            foreach (var send in ctx.LegacyEmails)
            {
                await send(emailService);
            }

            logger.LogInformation(
                "Изменён план поселения {targetId}: операция {operation}, аргументы {arguments}",
                targetId,
                operationName,
                arguments);

            return result;
        }
        catch (Exception e)
        {
            logger.LogWarning(
                e,
                "Не удалось изменить план поселения {targetId}: операция {operation}, аргументы {arguments}",
                targetId,
                operationName,
                arguments);
            throw;
        }
    }
}
