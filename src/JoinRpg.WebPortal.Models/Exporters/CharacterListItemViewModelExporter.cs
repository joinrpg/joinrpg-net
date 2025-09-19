using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.Exporters;

public class CharacterListItemViewModelExporter(ProjectInfo projectInfo, IUriService uriService) : CustomExporter<CharacterListItemViewModel>(uriService)
{
    public override IEnumerable<ITableColumn> ParseColumns()
    {
        yield return StringColumn(x => x.Name);
        yield return UriColumn(x => x, "Персонаж");
        yield return EnumColumn(x => x.BusyStatus);
        yield return StringListColumn(x => x.Groups.ParentGroups.Select(g => g.Name), "Группы");
        yield return UriListColumn(x => x.Groups.ParentGroups);
        yield return ShortUserColumn(x => x.Responsible);

        foreach (var projectField in projectInfo.SortedFields.Where(f => f.CanHaveValue))
        {
            yield return FieldColumn(projectField, x => x.Fields);
        }


        foreach (var tableColumn in UserColumn(x => x.Player, projectInfo))
        {
            yield return tableColumn;
        }
    }
}
