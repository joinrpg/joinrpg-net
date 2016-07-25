using System.Collections.Generic;
using System.Linq;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Helpers
{
  public class ClaimListItemViewModelExporter: CustomExporter<ClaimListItemViewModel>
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
      yield return DateTimeColumn(x => x.UpdateDate);
      yield return IntColumn(x => x.FeeDue);
      yield return IntColumn(x => x.FeePaid);
      foreach (var c in ShortUserColumn(x => x.LastModifiedBy))
      {
        yield return c;
      }
      foreach (var c in ShortUserColumn(x => x.Responsible))
      {
        yield return c;
      }
      foreach (var c in UserColumn(x => x.Player))
      {
        yield return c;
      }

      foreach (var projectField in Fields.Where(f => f.CanHaveValue()))
      {
        yield return
          FieldColumn(projectField.FieldName,
            x => x.Fields.FieldById(projectField.ProjectFieldId)?.DisplayString);
      }
    }

  }
}