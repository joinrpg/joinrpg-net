using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl.Allrpg
{
  internal static class AllrpgImportUtilities
  {
    private static string GetFioComponent(string present, IReadOnlyList<string> splitFio, int index)
    {
      return string.IsNullOrWhiteSpace(present) && splitFio.Count > index ? splitFio[index] : present;
    }

    public static void ImportUserFromResult(User user, ProfileReply result)
    {
      user.Auth = user.Auth ?? new UserAuthDetails();
      user.Extra = user.Extra ?? new UserExtra();

      user.Allrpg.Sid = result.sid;

      var splitFio = result.fio.Trim().Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
      user.SurName = GetFioComponent(user.SurName, splitFio, 0);
      user.BornName = GetFioComponent(user.BornName, splitFio, 1);
      user.FatherName = GetFioComponent(user.FatherName, splitFio, 2);

      if (user.Extra.Gender == Gender.Unknown && result.gender <= 2)
      {
        user.Extra.GenderByte = result.gender;
      }
      if (user.Extra.PhoneNumber == null)
      {
        user.Extra.PhoneNumber = result.phone2;
      }
      if (user.Extra.Nicknames == null)
      {
        user.Extra.Nicknames = result.nick;
      }

      if (user.Extra.Skype == null)
      {
        user.Extra.Skype = result.skype;
      }

      if (user.Extra.GroupNames == null)
      {
        user.Extra.GroupNames = result.ingroup;
      }

      if (user.Extra.BirthDate == null)
      {
        user.Extra.BirthDate = result.birth;
      }

      user.Auth.RegisterDate = new[] {user.Auth.RegisterDate, result.CreateDate}.Min();
    }
  }
}