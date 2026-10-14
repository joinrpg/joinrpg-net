using JoinRpg.DomainTypes;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>
/// Колонка выгрузки мастера и поле проекта, которому она соответствует.
/// </summary>
/// <param name="Header">Заголовок колонки — как в файле.</param>
/// <param name="Field">Поле проекта; null — колонка не сопоставлена (служебная или неоднозначная).</param>
/// <param name="Warnings">Пометки сопоставления.</param>
public record MasterExportColumnMapping(
    string Header,
    ProjectFieldIdentification? Field,
    IReadOnlyList<MasterExportFieldWarning> Warnings)
{
    /// <summary>Колонку можно брать без вопросов: поле найдено и пометок нет.</summary>
    public bool ApplyByDefault => Field is not null && Warnings.Count == 0;
}
