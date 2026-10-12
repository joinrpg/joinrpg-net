using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Fields;

public class ProjectFieldSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }

    [Display(Name = "Имя персонажа привязано к", Description = "Привязывать можно только к полям персонажа типа строка.")]
    public required ProjectFieldIdentification? NameField { get; set; }

    [Display(Name = "Описание персонажа привязано к", Description = "Привязывать можно только к полям персонажа типа текст.")]
    public required ProjectFieldIdentification? DescriptionField { get; set; }
}
