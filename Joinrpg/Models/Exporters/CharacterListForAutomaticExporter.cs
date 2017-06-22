using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters
{
  public class CharacterListForAutomaticExporter : CharacterListExporterBase
  {
    public CharacterListForAutomaticExporter(IReadOnlyCollection<ProjectField> fields, IUriService uriService) : base(fields, uriService)
    {
    }

    public override IEnumerable<ITableColumn> ParseColumns()
    {
      yield return IntColumn(x => x.CharacterId);

      foreach (var tableColumn in BasicColumns())
      {
        yield return tableColumn;
      }

      yield return UriListColumn(x => x.Groups.ParentGroups);

      foreach (var projectField in Fields)
      {
        yield return
          FieldColumn(projectField, x => x.Fields, $"{projectField.ProjectFieldId} {projectField.FieldName}");
      }


      yield return ComplexElementMemberColumn(x => x.Player, p => p.UserId);
      
      foreach (var tableColumn in UserColumn(x => x.Player))
      {
        yield return tableColumn;
      }
    }
  }
}