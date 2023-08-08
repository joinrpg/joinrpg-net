using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectMasterTools.Subscribe;

namespace JoinRpg.Web.Models.Subscribe;

public static class Builders
{
    public static SubscribeListViewModel ToSubscribeListViewModel(
        this (User User, UserSubscriptionDto[] UserSubscriptions) data,
        User currentUser,
        IUriService uriService,
        int projectId,
        List<PaymentTypeDto> paymentTypes)
    {
        return new SubscribeListViewModel()
        {
            AllowChanges = data.User.UserId == currentUser.UserId, //TODO allow project admins to setup subscribe for other masters
            Items = data.UserSubscriptions.Select(x => x.ToViewModel(uriService)).ToList(),
            ProjectId = projectId,
            MasterId = data.User.UserId,
            PaymentTypeNames = paymentTypes.Select(pt => ((PaymentTypeKindViewModel)pt.TypeKind).GetDisplayName(data.User)).ToArray(),
        };
    }

    private record class AbstractLinkable
        (LinkType LinkType, string Identification, int? ProjectId, string Name)
        : ISubscribeTarget
    {

    }

    public static SubscribeListItemViewModel ToViewModel(this UserSubscriptionDto dto, IUriService uriService)
    {
        var link = dto.ToSubscribeTargetLink();
        return new SubscribeListItemViewModel
        {
            Name = link.Name,
            Link = uriService.GetUri(link),
            UserSubscriptionId = dto.UserSubscriptionId,
            ProjectId = dto.ProjectId,
            Options = new SubscribeOptionsViewModel()
            {
                AccommodationChange = dto.Options.AccommodationChange,
                ClaimStatusChange = dto.Options.ClaimStatusChange,
                Comments = dto.Options.Comments,
                FieldChange = dto.Options.FieldChange,
                MoneyOperation = dto.Options.MoneyOperation,
            },
        };
    }

    public static ISubscribeTarget ToSubscribeTargetLink(this UserSubscriptionDto dto)
    {
        (LinkType type, string name, int id) link = dto switch
        {
            { CharacterId: int id, CharacterNames: string name } => (LinkType.ResultCharacter, name, id),
            { CharacterGroupId: int id, CharacterGroupName: string name } => (LinkType.ResultCharacterGroup, name, id),
            { ClaimId: int id, ClaimName: string name } => (LinkType.Claim, name, id),
            _ => throw new InvalidOperationException(),
        };

        return new AbstractLinkable(
            link.type,
            link.id.ToString(),
            dto.ProjectId,
            link.name);
    }

}
