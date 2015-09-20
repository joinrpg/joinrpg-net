using System;
using System.Collections.Generic;
using System.Linq;
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

    public virtual ICollection<ProjectAcl> ProjectAcls { get; set; }

    public virtual ICollection<Claim> Claims { get; set; }

    public string DisplayName
      => new string[] {PrefferedName, FullName, Email}.SkipWhile(string.IsNullOrWhiteSpace).FirstOrDefault();

    public string FullName => new[] {BornName, FatherName, SurName}.JoinIfNotNullOrWhitespace(" ");

    public string PrefferedName { get; set; }

    public virtual UserAuthDetails Auth { get; set; }

    public virtual AllrpgUserDetails Allrpg { get; set; }
    public virtual UserExtra Extra { get; set; }

    public virtual ICollection<UserSubscription> Subscriptions{ get; set; }
  }

  public enum Gender : byte
  {
    Unknown = 0,
    Male = 1,
    Female = 2
  }

  public class UserExtra
  {
    public int UserId { get; set; }
    public byte GenderByte { get; set; }

    public Gender Gender
    {
      get { return (Gender) GenderByte; }
      set { GenderByte = (byte) value; }
    }

    public string PhoneNumber { get; set; }
    public string Skype { get; set; }

    public string Nicknames { get; set; }

    public string GroupNames { get; set; }

    public DateTime? BirthDate { get; set; }
  }



  public class UserAuthDetails
  {
    public int UserId { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime RegisterDate { get; set; }
  }

  public class AllrpgUserDetails
  {
    public int UserId { get; set; }
    public int? Sid { get; set; }
    public string JsonProfile { get; set; }
  }
}