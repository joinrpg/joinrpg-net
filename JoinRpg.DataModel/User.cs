using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;

namespace JoinRpg.DataModel
{
  public class User : IUser<int>
  {
    public int UserId
    { get; set; }

    public string BornName
    { get; set; }

    public string FatherName
    { get; set; }

    public string SurName
    { get; set; }

    public int? LegacyAllRpgInp
    { get; set; }

    public int Id => UserId;
    public string UserName { get; set; }
    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public string PhoneNumber { get; set; }

    public virtual ICollection<ProjectAcl> ProjectAcls { get; set; }
  }
}
