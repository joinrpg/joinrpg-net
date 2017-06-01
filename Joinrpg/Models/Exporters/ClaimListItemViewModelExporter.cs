using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Exporters
{
  public class ClaimListItemViewModelExporter: CustomExporter<ClaimListItemViewModel>
  {
    public ClaimListItemViewModelExporter(ICollection<ProjectField> fields, IUriService uriService) : base(uriService)
    {
      Fields = fields;
    }

    private ICollection<ProjectField> Fields { get; }

    public  override IEnumerable<ITableColumn> ParseColumns()
    {
      yield return StringColumn(x => x.Name);
      yield return UriColumn(x => x);
      yield return EnumColumn(x => x.ClaimStatus);
      yield return DateTimeColumn(x => x.UpdateDate);
      yield return DateTimeColumn(x => x.CreateDate);
      yield return IntColumn(x => x.FeeDue);
      yield return IntColumn(x => x.FeePaid);
      yield return ShortUserColumn(x => x.LastModifiedBy);
      yield return ShortUserColumn(x => x.Responsible);
      foreach (var c in UserColumn(x => x.Player))
      {
        yield return c;
      }

      foreach (var projectField in Fields.Where(f => f.CanHaveValue()))
      {
        yield return
          FieldColumn(projectField, x => x.Fields);
      }
    }
  }
}