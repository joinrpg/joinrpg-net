using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.Exporters;

public abstract class CharacterListExporterBase : CustomExporter<CharacterListItemViewModel>
{
    protected CharacterListExporterBase(ProjectInfo projectInfo,
        IUriService uriService)
        : base(uriService)
    {
        ProjectInfo = projectInfo;
    }

    protected ProjectInfo ProjectInfo { get; }

    protected IEnumerable<ITableColumn> BasicColumns()
    {
        yield return StringColumn(x => x.Name);
        yield return UriColumn(x => x, "Персонаж");
        yield return EnumColumn(x => x.BusyStatus);
        yield return StringListColumn(x => x.Groups.ParentGroups.Select(g => g.Name), "Группы");
        yield return UriListColumn(x => x.Groups.ParentGroups);
        yield return ShortUserColumn(x => x.Responsible);
    }
}
