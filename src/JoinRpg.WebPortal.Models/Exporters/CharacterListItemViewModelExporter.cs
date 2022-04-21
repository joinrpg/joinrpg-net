using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters;

public class CharacterListItemViewModelExporter : CharacterListExporterBase
{

    public CharacterListItemViewModelExporter(IReadOnlyCollection<ProjectField> fields, IUriService uriService) :
      base(fields, uriService)
    {
    }

    public override IEnumerable<ITableColumn> ParseColumns()
    {
        foreach (var tableColumn in BasicColumns())
        {
            yield return tableColumn;
        }

        foreach (var projectField in Fields)
        {
            yield return
              FieldColumn(projectField, x => x.Fields);
        }


        foreach (var tableColumn in UserColumn(x => x.Player))
        {
            yield return tableColumn;
        }
    }
}
