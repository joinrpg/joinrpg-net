using System.Collections.Generic;
using JoinRpg.Helpers;
using Microsoft.AspNet.Identity;

namespace JoinRpg.DataModel
{
  public class User : IUser<int>
  {
    public int UserId { get; set; }

    public string BornName { get; set; }

    public string FatherName { get; set; }

    public string SurName { get; set; }

    public int Id => UserId;
    public string UserName { get; set; }
    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public string PhoneNumber { get; set; }

    public virtual ICollection<ProjectAcl> ProjectAcls { get; set; }

    public virtual ICollection<Claim> Claims { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Email : FullName; //Should create creative display name

    public string FullName => new[] {BornName, FatherName, SurName}.JoinIfNotNullOrWhitespace(" ");

    public virtual UserAuthDetails Auth { get; set; }
  }

  public class UserAuthDetails
  {
    public int UserId { get; set; }
    public int? LegacyAllRpgInp { get; set; }

    public bool EmailConfirmed { get; set; }
  }
}