using System;
using System.Linq;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Subscribe
{
    public class SubscribeListViewModel
    {
        public UserProfileDetailsViewModel User { get; set; }
        public SubscribeListItemViewModel[] Items { get; set; }

        public bool AllowChanges { get; set; }
    }

    public static class Builders
    {
        public static SubscribeListViewModel ToSubscribeListViewModel(
            this (User User, UserSubscriptionDto[] UserSubscriptions) data,
            User currentUser,
            IUriService uriService)
        {
            return new SubscribeListViewModel()
            {
                User = new UserProfileDetailsViewModel(data.User, currentUser),
                AllowChanges = data.User == currentUser, //TODO allow project admins to setup subscribe for other masters
                Items = data.UserSubscriptions.Select(x => x.ToViewModel(uriService)).ToArray(),
            };
        }

        private class AbstractLinkable : ILinkable
        {
            public LinkType LinkType { get; set; }

            public string Identification { get; set; }

            public int? ProjectId { get; set; }
        }

        public static SubscribeListItemViewModel ToViewModel(this UserSubscriptionDto dto, IUriService uriService)
        {
            (LinkType type, string name, int id) link = dto switch
            {
                { CharacterId: int id, CharacterNames: string name } => (LinkType.ResultCharacter, name, id),
                { CharacterGroupId: int id, CharacterGroupName: string name } => (LinkType.ResultCharacterGroup, name, id),
                { ClaimId: int id, ClaimName: string name } => (LinkType.Claim, name, id),
                _ => throw new InvalidOperationException(),
            };

            return new SubscribeListItemViewModel
            {
                Name = link.name,
                Link = uriService.GetUri(new AbstractLinkable
                {
                    LinkType = link.type,
                    Identification = link.id.ToString(),
                    ProjectId = dto.ProjectId,
                }),
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

    }
}
