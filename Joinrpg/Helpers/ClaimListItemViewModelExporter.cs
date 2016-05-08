using System.Collections.Generic;
using System.Linq;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Helpers
{
  public class ClaimListItemViewModelExporter: CustomerExporter<ClaimListItemViewModel>
  {
    public ClaimListItemViewModelExporter(ICollection<ProjectField> fields)
    {
      Fields = fields;
    }

    private ICollection<ProjectField> Fields { get; }

    public  override IEnumerable<ITableColumn> ParseColumns()
    {
      yield return StringColumn(x => x.Name);
      yield return EnumColumn(x => x.ClaimStatus);
      yield return DateTimeCOlumn(x => x.UpdateDate);
      yield return UserColumn(x => x.LastModifiedBy);
      yield return UserColumn(x => x.Responsible);
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