using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Allrpg;

namespace JoinRpg.Services.Impl.Allrpg
{
  [UsedImplicitly]
  public class AllrpgServiceImpl : DbServiceImplBase, IAllrpgService
  {
    private readonly AllrpgApi _api;

    public async Task<DownloadResult> DownloadAllrpgProfile(int userId)
    {
      var user =
        await UserRepository.WithProfile(userId);

      if (user.Allrpg?.Sid != null)
      {
        return DownloadResult.AlreadyDownloaded;
      }

      var reply = await _api.GetProfile(user.Email);

      switch (reply.Status)
      {
        case AllrpgApi.Status.Success:
          break;
        case AllrpgApi.Status.NetworkError:
          return DownloadResult.NetworkError;
        case AllrpgApi.Status.ParseError:
          return DownloadResult.ParseError;
        case AllrpgApi.Status.NoSuchUser:
          user.Allrpg = user.Allrpg ?? new AllrpgUserDetails();
          user.Allrpg.Sid = 0;
          user.Allrpg.PreventAllrpgPassword = true;
          await UnitOfWork.SaveChangesAsync();
          return DownloadResult.Success;
        case AllrpgApi.Status.WrongKey:
          return DownloadResult.WrongKey;
        default:
          throw new ArgumentOutOfRangeException();
      }

      var result = reply.Result;

      user.Allrpg = user.Allrpg ?? new AllrpgUserDetails();
      user.Extra = user.Extra ?? new UserExtra();

      if (user.PasswordHash != null)
      {
        user.Allrpg.PreventAllrpgPassword = true;
      }

      user.Allrpg.JsonProfile = reply.RawResult; //Save raw value, we m.b. like to parse it later

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

      await UnitOfWork.SaveChangesAsync();
      return DownloadResult.Success;
    }

    public async Task<LegacyLoginResult> TryToLoginWithOldPassword(string email, string password)
    {
      var user = await UserRepository.GetByEmail(email);
      if (user == null)
      {
        //TODO: try to import user from scratch
        return LegacyLoginResult.NoSuchUserOrPassword;
      }

      if (user.PasswordHash != null || user.Allrpg?.Sid == null || user.Allrpg?.PreventAllrpgPassword == true)
      {
        return LegacyLoginResult.ImportDisabled;
      }

      var reply = await  _api.CheckPassword(email, password);


      switch (reply.Status)
      {
        case AllrpgApi.Status.Success:
          return reply.Result.Success ? LegacyLoginResult.Success : LegacyLoginResult.NoSuchUserOrPassword;
        case AllrpgApi.Status.NetworkError:
          return LegacyLoginResult.NetworkError;
        case AllrpgApi.Status.ParseError:
          return LegacyLoginResult.ParseError;
        case AllrpgApi.Status.NoSuchUser:
          return LegacyLoginResult.NoSuchUserOrPassword;
        case AllrpgApi.Status.WrongKey:
          return LegacyLoginResult.WrongKey;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private static string GetFioComponent(string present, IReadOnlyList<string> splitFio, int index)
    {
      return string.IsNullOrWhiteSpace(present) && splitFio.Count > index ? splitFio[index] : present;
    }

    public AllrpgServiceImpl(IUnitOfWork unitOfWork, IAllrpgApiKeyStorage keyStorage) : base(unitOfWork)
    {
      _api = new AllrpgApi(keyStorage.Key);
    }
  }
}
