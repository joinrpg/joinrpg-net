using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Claims;

public class InvitePlayerViewModel
{
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Выберите персонажа")]
    [DisplayName("Персонаж")]
    public int CharacterId { get; set; }

    [MinLength(1, ErrorMessage = "Введите хотя бы одну ссылку на пользователя")]
    [Display(Name = "Ссылки на пользователей",
        Description = "ID пользователя, ссылка на профиль (например: 123, /user/123, https://joinrpg.ru/user/123), ссылка VK (vk.com/id…), Telegram (@username или t.me/username) или email")]
    public List<string> UserLinks { get; set; } = [];

    [Display(Name = "Сообщение игроку",
         Description = "Все, что вы хотите сообщить игроку дополнительно")]
    public string ClaimText { get; set; } = "Мастер пригласил вас на роль";

    public string ProjectName { get; set; } = "";

    public CharacterIdentification CharacterIdentification => new CharacterIdentification(new ProjectIdentification(ProjectId), CharacterId);
}
