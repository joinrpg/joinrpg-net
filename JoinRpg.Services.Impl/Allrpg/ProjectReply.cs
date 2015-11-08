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
    [NotNull,ItemNotNull]
    public ICollection<ProfileReply> users { get; set; }
    [NotNull, ItemNotNull]
    public ICollection<LocationData> locations { get; set; }

    [NotNull, ItemNotNull]
    public ICollection<VacancyData> vacancies { get; set; }
  }

  internal class LocationData
  {
    public int id { get; set; }
    public int parent { get; set; }
    [NotNull]
    public string name { get; set; }
    public int rights { get; set; }
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
}