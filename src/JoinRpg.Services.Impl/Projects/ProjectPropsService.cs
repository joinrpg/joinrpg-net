using System.Runtime.CompilerServices;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl.Projects;

internal class ProjectPropsService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository metadataRepository,
    ILogger<ProjectPropsService> logger)
    : DbServiceImplBase(unitOfWork, currentUserAccessor), IProjectPropsService
{
    public Task ChangeProjectProperties<TArgs>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<ProjectOperationContext<TArgs>> action,
        [CallerMemberName] string operationName = "")
        => ChangeProjectPropertiesCore(projectId, requiredPermission, activeRequirement, arguments,
            AsFunc(action), operationName);

    public Task<TResult> ChangeProjectProperties<TArgs, TResult>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ProjectOperationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "")
        => ChangeProjectPropertiesCore(projectId, requiredPermission, activeRequirement, arguments,
            action, operationName);

    private static Func<ProjectOperationContext<TArgs>, bool> AsFunc<TArgs>(Action<ProjectOperationContext<TArgs>> action)
        => ctx =>
        {
            action(ctx);
            return true;
        };

    private async Task<TResult> ChangeProjectPropertiesCore<TArgs, TResult>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ProjectOperationContext<TArgs>, TResult> action,
        string operationName)
    {
        try
        {
            // Write-репозиторий берём из UnitOfWork: он обязан использовать тот же DbContext, через
            // который мы потом сохраняем, иначе мутация трекается в одном контексте, а SaveChanges
            // вызывается на другом (см. интеграционный тест AcceptInvitationScenario).
            var handle = await UnitOfWork.GetProjectMetadataWriteRepository().LoadProjectForUpdate(projectId);

            // Админ (в т.ч. робот, под которым выполняются фоновые джобы) проходит проверку прав.
            if (!IsCurrentUserAdmin)
            {
                _ = handle.ProjectInfo.RequestMasterAccess(currentUserAccessor, requiredPermission);
            }

            if (activeRequirement == ProjectActiveRequirement.MustBeActive)
            {
                _ = handle.ProjectInfo.EnsureProjectActive();
            }

            var ctx = new ProjectOperationContext<TArgs>(handle.Project, handle.ProjectInfo, Now, currentUserAccessor, arguments);
            var result = action(ctx);

            await UnitOfWork.SaveChangesAsync();

            // Project изменился — пересобираем ProjectInfo и обновляем кэш, чтобы повторные
            // чтения в рамках того же запроса видели актуальное состояние.
            metadataRepository.PrimeCache(handle.Refresh());

            logger.LogInformation(
                "Изменены метаданные проекта {projectId}: операция {operation}, аргументы {@arguments}",
                projectId,
                operationName,
                arguments);

            return result;
        }
        catch (Exception e)
        {
            logger.LogWarning(
                e,
                "Не удалось изменить метаданные проекта {projectId}: операция {operation}, аргументы {@arguments}",
                projectId,
                operationName,
                arguments);
            throw;
        }
    }
}
