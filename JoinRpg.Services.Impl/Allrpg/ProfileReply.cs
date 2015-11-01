using System;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.Services.Impl.Allrpg
{
  [UsedImplicitly]
  internal class ProfileReply
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