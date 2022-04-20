using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
    public class ProjectFeeSettingListItemViewModel : ProjectFeeSettingViewModelBase
    {
        public bool IsActual { get; }
        public int ProjectFeeSettingId { get; }

        public ProjectFeeSettingListItemViewModel(ProjectFeeSetting fs)
        {
            Fee = fs.Fee;
            PreferentialFee = fs.PreferentialFee;
            StartDate = fs.StartDate;
            IsActual = fs.StartDate > DateTime.UtcNow;
            ProjectFeeSettingId = fs.ProjectFeeSettingId;
            ProjectId = fs.ProjectId;
        }
    }
}
