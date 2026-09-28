using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Web.Models;

public class ProjectFeeSettingListItemViewModel : ProjectFeeSettingViewModelBase
{
    public bool IsActual { get; }
    public int ProjectFeeSettingId { get; }

    public ProjectFeeSettingListItemViewModel(ProjectFeeSettingInfo fs, ProjectIdentification projectId)
    {
        Fee = fs.Fee;
        PreferentialFee = fs.PreferentialFee;
        StartDate = DateOnly.FromDateTime(fs.StartDate);
        IsActual = fs.StartDate > DateTime.UtcNow;
        ProjectFeeSettingId = fs.ProjectFeeSettingId;
        ProjectId = projectId.Value;
    }
}
