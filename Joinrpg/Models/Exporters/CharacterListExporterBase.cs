using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.Exporters
{
  public abstract class CharacterListExporterBase : CustomExporter<CharacterListItemViewModel>
  {
    protected CharacterListExporterBase(IReadOnlyCollection<ProjectField> fields, IUriService uriService)
      : base(uriService)
    {
      Fields = fields;
    }
    protected IReadOnlyCollection<ProjectField> Fields { get; }

    protected IEnumerable<ITableColumn> BasicColumns()
    {
      yield return StringColumn(x => x.Name);
      yield return UriColumn(x => x);
      yield return EnumColumn(x => x.BusyStatus);
      yield return StringListColumn(x => x.Groups.ParentGroups.Select(g => g.Name));
    }
  }
}