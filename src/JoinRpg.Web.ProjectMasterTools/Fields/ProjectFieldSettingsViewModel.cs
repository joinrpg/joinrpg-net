using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Fields;

public class ProjectFieldSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }

    /// <summary>Есть право менять поля; без него панель только для просмотра.</summary>
    public required bool HasEditAccess { get; init; }

    [Display(Name = "Имя персонажа привязано к", Description = "Привязывать можно только к полям персонажа типа строка.")]
    public required ProjectFieldIdentification? NameField { get; set; }

    [Display(Name = "Описание персонажа привязано к", Description = "Привязывать можно только к полям персонажа типа текст.")]
    public required ProjectFieldIdentification? DescriptionField { get; set; }

    /// <summary>
    /// Поля, похожие на имя персонажа, — пока поле-имя не выбрано (<see cref="NameField"/> == null),
    /// панель предупреждает о них. Только для показа, при сохранении не используется.
    /// </summary>
    public IReadOnlyCollection<string> NameFieldCandidates { get; set; } = [];
}
