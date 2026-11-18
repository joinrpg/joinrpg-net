using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.ClaimList;

namespace JoinRpg.Web.Models.Exporters;

public class ClaimListItemViewModelExporter(IUriService uriService, ProjectInfo projectInfo) : CustomExporter<ClaimListItemForExportViewModel>(uriService)
{
    public override IEnumerable<ITableColumn> ParseColumns()
    {
        yield return StringColumn(x => x.Name);
        yield return UriColumn(x => x);
        yield return EnumColumn(x => x.ClaimFullStatusView.ClaimStatus);
        yield return EnumColumn(x => x.ClaimFullStatusView.ClaimDenialStatus);
        yield return DateTimeColumn(x => x.UpdateDate);
        yield return DateTimeColumn(x => x.CreateDate);
        yield return DateTimeColumn(x => x.CheckInDate);
        yield return IntColumn(x => x.TotalFee);
        yield return IntColumn(x => x.FeeDue);
        yield return IntColumn(x => x.FeePaid);
        if (projectInfo.ProjectFinanceSettings.PreferentialFeeEnabled)
        {
            yield return BoolColumn(x => x.PreferentialFeeUser);
        }
        if (projectInfo.AccommodationSettings.Enabled)
        {
            yield return StringColumn(x => x.AccomodationType);
            yield return StringColumn(x => x.RoomName);
        }

        yield return ShortUserColumn(x => x.LastModifiedBy);
        yield return ShortUserColumn(x => x.Responsible);
        foreach (var c in UserColumn(x => x.FullPlayer, projectInfo))
        {
            yield return c;
        }

        var requirements = projectInfo.ProfileRequirementSettings;
        // Паспорт и адрес — парой: так их узнаёт парсер выгрузки DRP (MasterExportColumns.PassportBlock).
        if ((requirements.PassportRequested || requirements.RegistrationAddressRequested) && !projectInfo.IsArchived)
        {
            yield return StringColumn(x => x.PassportData);
            yield return StringColumn(x => x.RegistrationAddress);
        }
        if (requirements.BirthDateRequested && !projectInfo.IsArchived)
        {
            yield return DateOnlyColumn(x => x.BirthDate);
        }

        foreach (var projectField in projectInfo.SortedFields.Where(f => f.CanHaveValue))
        {
            yield return FieldColumn(projectField, x => x.FieldValues);
        }
    }
}
