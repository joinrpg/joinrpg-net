using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Helpers
{
  public class CharacterListItemViewModelExporter : CustomExporter<CharacterListItemViewModel>
  {
    public CharacterListItemViewModelExporter(IReadOnlyCollection<ProjectField> fields)
    {
      Fields = fields;
    }

    private IReadOnlyCollection<ProjectField> Fields { get; }

    public override IEnumerable<ITableColumn> ParseColumns()
    {
      yield return StringColumn(x => x.Name);


      foreach (var projectField in Fields)
      {
        yield return
          FieldColumn(projectField.FieldName,
            x => x.Fields.Fields.SingleOrDefault(f => f.ProjectFieldId == projectField.ProjectFieldId)?.DisplayString);
      }

      yield return EnumColumn(x => x.BusyStatus);
      foreach (var c in UserColumn(x => x.Player))
      {
        yield return c;
      }
    }

  }
}