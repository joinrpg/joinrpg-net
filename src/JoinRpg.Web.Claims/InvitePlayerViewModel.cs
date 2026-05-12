using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Claims;

public class InvitePlayerViewModel
{
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Выберите персонажа")]
    [DisplayName("Персонаж")]
    public int CharacterId { get; set; }

    [Required(ErrorMessage = "Введите ссылку на пользователя")]
    [DisplayName("Ссылка на пользователя")]
    [Description("ID пользователя, относительный или полный URL (например: 123, /users/123, https://joinrpg.ru/users/123)")]
    public string UserLink { get; set; } = "";

    public string ProjectName { get; set; } = "";

    public CharacterIdentification CharacterIdentification => new CharacterIdentification(new ProjectIdentification(ProjectId), CharacterId);
}
