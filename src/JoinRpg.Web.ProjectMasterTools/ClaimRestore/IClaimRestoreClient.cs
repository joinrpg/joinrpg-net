namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>
/// Восстановление проекта после потери основной БД: заявки из базы уведомлений (DRP, #5481), группы,
/// персонажи и поля из выгрузок мастера (#5529). Только в заблокированном проекте (ADR023).
/// </summary>
/// <remarks>
/// Выгрузки на сервере не хранятся: <see cref="ParseExport"/> отдаёт разобранную выгрузку непрозрачной
/// строкой, остров держит её у себя и присылает в <see cref="GetPlan"/>.
/// </remarks>
public interface IClaimRestoreClient
{
    Task<ClaimRestorePlanViewModel> GetPlan(ClaimRestorePlanRequest request);

    /// <summary>Разбирает загруженный файл выгрузки мастера (только xlsx).</summary>
    Task<MasterExportUploadResult> ParseExport(ProjectIdentification projectId, string fileName, Stream content);

    /// <summary>
    /// Применяет одну строку плана заявок. Каждая строка — отдельный запрос: после неудачной строки
    /// следующая начинается с чистого контекста.
    /// </summary>
    Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request);

    /// <summary>Создаёт потерянную группу с прежним id.</summary>
    Task ApplyGroup(GroupRestoreApplyRequest request);

    /// <summary>Создаёт потерянного персонажа с прежним id или меняет существующего.</summary>
    Task ApplyCharacter(CharacterRestoreApplyRequest request);

    /// <summary>Записывает в заявку поля из выгрузки.</summary>
    Task ApplyClaimFields(ClaimFieldsRestoreApplyRequest request);
}
