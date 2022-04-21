using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Areas.Admin.Models;

public class ChangeEmailModel
{
    [Required]
    public int UserId { get; set; }
    [Required, EmailAddress, Display(Name = "Новый адрес email")]
    public string NewEmail { get; set; }
}
