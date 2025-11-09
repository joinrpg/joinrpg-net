using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Claims;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.ClaimList;

/// <summary>
/// Use this ctor if need to export claims from different projects
/// </summary>
/// <param name="currentUserId"></param>
/// <param name="claimPair"></param>
public class ClaimListForExportViewModel(ICurrentUserAccessor currentUserId, IReadOnlyCollection<(Claim Claim, ProjectInfo ProjectInfo)> claimPair)
{
    public IEnumerable<ClaimListItemForExportViewModel> Items { get; } = claimPair
          .Select(c => ClaimListBuilder.BuildItemForExport(c.Claim, currentUserId, c.ProjectInfo))
          .ToList();

    public ClaimListForExportViewModel(ICurrentUserAccessor currentUserId, IReadOnlyCollection<Claim> claims, ProjectInfo projectInfo)
        : this(currentUserId, claims.Select(c => (c, projectInfo)).ToList())
    {
    }
}
public record class ClaimListItemForExportViewModel(
    [property: Display(Name = "Имя")] string Name,
    [property: Display(Name = "Игрок")] UserLinkViewModel Player,
    [property: Display(Name = "Игра")] string ProjectName,
    [property: Display(Name = "Статус")] ClaimFullStatusView ClaimFullStatusView,
    [property: Display(Name = "Обновлена"), UIHint("EventTime")] DateTime? UpdateDate,
    [property: Display(Name = "Создана"), UIHint("EventTime")] DateTime? CreateDate,
    [property: Display(Name = "Дата заезда")] DateTime? CheckInDate,
    [property: Display(Name = "Ответственный")] UserLinkViewModel Responsible,

    [property: Display(Name = "Уплачено")] int FeePaid,
    [property: Display(Name = "Осталось")] int FeeDue,
    [property: Display(Name = "Итого взнос")] int TotalFee,
    UserLinkViewModel? LastModifiedBy,
    ClaimIdentification ClaimId,
    [property: Display(Name = "Тип поселения")] string? AccomodationType,
    [property: Display(Name = "Комната")] string? RoomName,
    [property: Display(Name = "Льготник")] bool PreferentialFeeUser,
    [property: Display(Name = "Паспортные данные")] string? PassportData,
    [property: Display(Name = "Адрес")] string? RegistrationAddress,
    IReadOnlyDictionary<ProjectFieldIdentification, string> FieldValues,
    User FullPlayer // Этому объекту не место в viewmodel
    ) : ILinkable
{
    #region Implementation of ILinkable

    LinkType ILinkable.LinkType => LinkType.Claim;
    string ILinkable.Identification => ClaimId.ClaimId.ToString();
    int? ILinkable.ProjectId => ClaimId.ProjectId;

    #endregion
}
