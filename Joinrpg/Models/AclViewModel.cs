using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Web.Models
{
  public class AclViewModel
  {
    [ReadOnly(true)]
    public int? ProjectAclId { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    [DisplayName("Может настраивать поля персонажа")]
    public bool CanChangeFields { get; set; }
    [DisplayName("Может настраивать свойства базы заявок")]
    public bool CanChangeProjectProperties { get; set; }
    [DisplayName("Может давать доступ другим мастерам")]
    public bool CanGrantRights { get; set; }
  }
}
