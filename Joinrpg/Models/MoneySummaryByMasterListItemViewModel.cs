using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class MoneySummaryByMasterListItemViewModel
  {
    public User Master { get; }
    public int Total { get; }

    public MoneySummaryByMasterListItemViewModel(int total, User master)
    {
      Master = master;
      Total = total;
    }
  }
}