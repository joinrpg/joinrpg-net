using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Helpers
{
  public class CharacterListItemViewModelExporter : CustomerExporter<CharacterListItemViewModel>
  {
    public CharacterListItemViewModelExporter(ICollection<ProjectField> fields)
    {
      Fields = fields;
    }

    private ICollection<ProjectField> Fields { get; }

    public override IEnumerable<ITableColumn> ParseColumns()
    {
      yield return StringColumn(x => x.Name);
      yield return EnumColumn(x => x.BusyStatus);
      yield return UserColumn(x => x.Player);

      foreach (var projectField in Fields)
      {
        yield return
          FieldColumn(projectField.FieldName,
            x => x.Fields.Fields.SingleOrDefault(f => f.ProjectFieldId == projectField.ProjectFieldId)?.DisplayString);
      }
    }

  }
}