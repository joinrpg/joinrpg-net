using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters;

public class CharacterListItemViewModelExporter : CharacterListExporterBase
{

    public CharacterListItemViewModelExporter(ProjectInfo projectInfo, IUriService uriService) :
      base(projectInfo, uriService)
    {
    }

    public override IEnumerable<ITableColumn> ParseColumns()
    {
        foreach (var tableColumn in BasicColumns())
        {
            yield return tableColumn;
        }

        foreach (var projectField in ProjectInfo.SortedFields.Where(f => f.CanHaveValue))
        {
            yield return FieldColumn(projectField, x => x.Fields);
        }


        foreach (var tableColumn in UserColumn(x => x.Player))
        {
            yield return tableColumn;
        }
    }
}
