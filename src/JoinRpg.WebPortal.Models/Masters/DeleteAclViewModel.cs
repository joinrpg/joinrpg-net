using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Models.Masters;

namespace JoinRpg.Web.Models;

public class DeleteAclViewModel
{
    [Display(
      Name = "Новый ответственный мастер",
      Description = "Ответственный мастер, который будет назначен тем заявкам, за которые раньше отвечал этот мастер.")]
    public int? ResponsibleMasterId { get; set; }

    public bool SelfRemove { get; set; }


    public int UserId { get; set; }

    [Display(Name = "Проект")]
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public AclViewModel InnerModel { get; set; } = null!;


    public int ProjectAclId { get; set; }

}
