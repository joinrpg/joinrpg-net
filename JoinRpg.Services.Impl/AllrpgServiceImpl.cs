using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Allrpg;
using Newtonsoft.Json;
using JoinRpg.Helpers;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class AllrpgServiceImpl : DbServiceImplBase, IAllrpgService
  {
    public async Task<DownloadResult> DownloadAllrpgProfile(int userId, string allrpgKey)
    {
      var user =
        await UserRepository.WithProfile(userId);

      if (user.Allrpg?.Sid != null)
      {
        return DownloadResult.AlreadyDownloaded;
      }
      string resultString;
      try
      {
        var bytes = Encoding.ASCII.GetBytes(user.Email + allrpgKey);
        var key = SHA1.Create().ComputeHash(bytes).Select(b => $"{b:x2}").AsString();

        resultString = await new WebClient().DownloadStringTaskAsync(
          new Uri(
            $"http://allrpg.info/joinrpg_profile.php?email={user.Email}&key={key}"));
      }
      catch (Exception)
      {
        return DownloadResult.NetworkError;
      }

      if (resultString == "ERROR_WRONG_KEY")
      {
        return DownloadResult.WrongKey;
      }

      if (resultString == "ERROR_NO_SUCH_USER")
      {
        user.Allrpg = user.Allrpg ?? new AllrpgUserDetails();
        user.Allrpg.Sid = 0;
        await UnitOfWork.SaveChangesAsync();
        return DownloadResult.Success;
      }

      AllrpgProfileImpl result;
      try
      {
        result = JsonConvert.DeserializeObject<AllrpgProfileImpl>(resultString);
      }
      catch
      {
        return DownloadResult.ParseError;
      }

      user.Allrpg = user.Allrpg ?? new AllrpgUserDetails();
      user.Extra = user.Extra ?? new UserExtra();

      user.Allrpg.JsonProfile = resultString; //Save raw value, we m.b. like to parse it later

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

    private static string GetFioComponent(string present, IReadOnlyList<string> splitFio, int index)
    {
      return string.IsNullOrWhiteSpace(present) && splitFio.Count > index ? splitFio[index] : present;
    }

    public AllrpgServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }



  [UsedImplicitly]
  internal class AllrpgProfileImpl
  {
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public int sid { get; set; }
    public string fio { get; set; }
    public string nick { get; set; }
    public byte gender { get; set; }
    public string em { get; set; }
    public string em2 { get; set; }
    public string phone2 { get; set; }
    public string icq { get; set; }
    public string skype { get; set; }
    public string jabber { get; set; }
    public string vkontakte { get; set; }
    public string livejournal { get; set; }
    public string googleplus { get; set; }
    public string facebook { get; set; }
    public string photo { get; set; }
    public string login { get; set; }
    public DateTime? birth { get; set; }
    public int city { get; set; }
    public string sickness { get; set; }
    public string additional { get; set; }
    public string prefer { get; set; }
    public string prefer2 { get; set; }
    public string prefer3 { get; set; }
    public string prefer4 { get; set; }
    public string specializ { get; set; }
    public string ingroup { get; set; }
    public string hidesome { get; set; }
    public long date { get; set; }

    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedAutoPropertyAccessor.Global

    public DateTime CreateDate => UnixTime.ToDateTime(date);
  }
}
