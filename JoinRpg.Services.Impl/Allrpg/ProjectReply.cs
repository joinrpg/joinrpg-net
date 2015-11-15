using System;
using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace JoinRpg.Services.Impl.Allrpg
{
  [UsedImplicitly]
  internal class ProjectReply
  {
    [NotNull]
    public string project_name { get; set; }

    [NotNull, ItemNotNull]
    public ICollection<ProfileReply> users { get; set; }

    [NotNull, ItemNotNull]
    public ICollection<LocationData> locations { get; set; }

    [NotNull, ItemNotNull]
    public ICollection<VacancyData> vacancies { get; set; }

    [NotNull, ItemNotNull]
    public ICollection<RoleData> roles { get; set; }

    [NotNull, ItemNotNull]
    public ICollection<CommentData> comments { get; set; }
  }

  internal class LocationData
  {
    public int id { get; set; }
    public int parent { get; set; }

    [NotNull]
    public string name { get; set; }

    public int rights { get; set; }
    /// <summary>
    /// This is a "order" field
    /// </summary>
    public int code { get; set; }

    [NotNull]
    public string description { get; set; }
  }

  internal class VacancyData
  {
    public int id { get; set; }
    public int locat { get; set; }

    [NotNull]
    public string name { get; set; }

    public int code { get; set; }
    public int kolvo { get; set; }
    public int autonewrole { get; set; }
    public string content { get; set; }
  }

  internal class RoleData
  {
    public int id { get; set; }
    public int sid { get; set; }
    public int vacancy { get; set; }
    public string money { get; set; }
    public int moneydone { get; set; }
    public string sorter { get; set; }
    public int locat { get; set; }
    public string allinfo { get; set; }
    public int status { get; set; }
    public int changed { get; set; }
    public int todelete { get; set; }
    public int todelete2 { get; set; }
    public int datesent { get; set; }
    public int date { get; set; }
    public IDictionary<int, string> @virtual { get; set; }
  }

  internal class CommentData
  {
    public int role_id { get; set; }
    public int type { get; set; }

    public int sid { get; set; }

    public int date { get; set; }
    public string content { get; set; }
  }
}