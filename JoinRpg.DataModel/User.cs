using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.Helpers;
using Microsoft.AspNet.Identity;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
  public class User : IUser<int>
  {
    public int UserId { get; set; }

    public override string ToString()
    {
      return
        $"User(UserId: {UserId}, BornName: {BornName}, FatherName: {FatherName}, SurName: {SurName}, Id: {Id}, UserName: {UserName}, Email: {Email}, PasswordHash: {PasswordHash}, ProjectAcls: {ProjectAcls}, Claims: {Claims}, DisplayName: {DisplayName}, FullName: {FullName}, PrefferedName: {PrefferedName}, Auth: {Auth}, Allrpg: {Allrpg}, Extra: {Extra}, Subscriptions: {Subscriptions})";
    }

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

    public virtual ICollection<UserExternalLogin> ExternalLogins { get; set; }
  }

  public enum Gender : byte
  {
    Unknown = 0,
    Male = 1,
    Female = 2
  }

  public class UserExternalLogin
  {
    public int UserExternalLoginId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string Provider { get; set; }
    public string Key { get; set; }
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

    public string Vk { get; set; }
    public string Livejournal { get; set; }

    public string Nicknames { get; set; }

    public string GroupNames { get; set; }

    public DateTime? BirthDate { get; set; }

    public override string ToString()
    {
      return $"UserExtra(UserId: {UserId}, Gender: {Gender}, PhoneNumber: {PhoneNumber}, Nicknames: {Nicknames}, GroupNames: {GroupNames}, BirthDate: {BirthDate})";
    }
  }



  public class UserAuthDetails
  {
    public int UserId { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime RegisterDate { get; set; }

    public bool IsAdmin { get; set; }

    public override string ToString()
    {
      return $"UserAuthDetails(UserId: {UserId}, EmailConfirmed: {EmailConfirmed}, RegisterDate: {RegisterDate})";
    }
  }

  public class AllrpgUserDetails
  {
    public int UserId { get; set; }
    public int? Sid { get; set; }
    public string JsonProfile { get; set; }

    public bool PreventAllrpgPassword { get; set; }

    public override string ToString()
    {
      return $"AllrpgUser(UserId: {UserId}, Sid: {Sid}, JsonProfile: {JsonProfile}, PreventAllrpgPassword: {PreventAllrpgPassword})";
    }
  }
}