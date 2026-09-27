namespace JoinRpg.Web.ProjectMasterTools.Fields;

/// <summary>
/// Navigation in fields area
/// </summary>
public class FieldNavigationModel
{
    /// <see cref="FieldNavigationPage"/>
    public FieldNavigationPage Page { get; set; }

    public int ProjectId { get; set; }

    /// <summary>
    /// Дефолтный шаблон заявки проекта, если он задан. На него ведёт ссылка «Посмотреть форму
    /// заявки»: мастеру показывается та же форма, что увидит игрок. Если шаблон не задан, ссылку
    /// не показываем — игроцкая страница «выберите роль» мастеру тут ничем не поможет.
    /// </summary>
    public CharacterIdentification? DefaultTemplateCharacterId { get; set; }
}

/// <summary>
/// Chosen page
/// </summary>
public enum FieldNavigationPage
{
    /// <summary>
    /// Some unknown page of this area
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// All active fields
    /// </summary>
    ActiveFieldsList = 1,
    /// <summary>
    /// All deleted fields
    /// </summary>
    DeletedFieldsList = 2,
    /// <summary>
    /// Field settings
    /// </summary>
    FieldSettings = 3,
    /// <summary>
    /// Adding field
    /// </summary>
    AddField = 4,
    /// <summary>
    /// Editing field
    /// </summary>
    EditField = 5,
}
