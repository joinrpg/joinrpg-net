namespace JoinRpg.Web.ProjectCommon;

public interface IProjectFieldUriLocator
{
    Uri GetEditUri(ProjectFieldIdentification fieldId);
    Uri GetCreateVariantUri(ProjectFieldIdentification fieldId);
    Uri GetEditVariantUri(ProjectFieldVariantIdentification variantId);

    /// <summary>Настройки полей проекта: поле-имя и поле-описание персонажа.</summary>
    Uri GetFieldSettingsUri(ProjectIdentification projectId);
}
