using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Автоприём заявки в проекте, где включён <c>AutoAcceptClaims</c>.
/// </summary>
/// <remarks>
/// <para>
/// Это <b>оркестратор, а не мутация</b>: собственной логики утверждения здесь нет, она целиком в
/// <see cref="IClaimApprovalService"/>. Сервис только перечитывает условия и, если они сошлись,
/// запускает утверждение от имени ответственного мастера.
/// </para>
/// <para>
/// До ADR014 автоприём вызывался из середины операции подачи заявки на <b>том же</b> экземпляре
/// сервиса, под <c>StartImpersonate</c>: <c>CurrentUserId</c> менялся посреди вызова, а <c>Now</c>
/// был зафиксирован в конструкторе <c>DbServiceImplBase</c>. Теперь автоприём вызывается строго
/// <b>после</b> завершения основной операции (§7). Порядок побочных эффектов сохранён: и раньше
/// автоприём шёл после рассылки уведомлений о создании заявки.
/// </para>
/// <para>
/// Условия читаются заново — из перечитанной заявки и <see cref="ProjectInfo"/>, а не из
/// мутированного EF-графа предыдущей операции.
/// </para>
/// </remarks>
internal class ClaimAutoApproveService(
    IClaimsRepository claimsRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IUserRepository userRepository,
    IImpersonateAccessor impersonateAccessor,
    IClaimApprovalService claimApprovalService,
    ILogger<ClaimAutoApproveService> logger)
{
    public async Task AutoApproveClaimIfNeeded(ClaimIdentification claimId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);

        if (!projectInfo.ClaimSettings.AutoAcceptClaims)
        {
            return;
        }

        var claim = await claimsRepository.GetClaim(claimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, nameof(Claim));

        // Не принимаем автоматически заявки, если игрок не предоставил доступ к паспорту
        if (!claim.PlayerAllowedSenstiveData && projectInfo.ProfileRequirementSettings.SensitiveDataRequired)
        {
            logger.LogInformation(
                "Claim ({claimId}) was not auto-approved: sensitive data access is required but not granted by player",
                claimId);
            return;
        }

        var responsibleMaster = await userRepository.GetRequiredUserInfo(
            new UserIdentification(claim.ResponsibleMasterUserId));

        impersonateAccessor.StartImpersonate(responsibleMaster.UserId, responsibleMaster.DisplayName, responsibleMaster.IsAdmin);
        try
        {
            //TODO[Localize]
            await claimApprovalService.ApproveByMaster(claimId, "Ваша заявка была принята автоматически");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claim ({claimId}) auto-approve failed", claimId);
            throw;
        }
        finally
        {
            impersonateAccessor.StopImpersonate();
        }
    }
}
