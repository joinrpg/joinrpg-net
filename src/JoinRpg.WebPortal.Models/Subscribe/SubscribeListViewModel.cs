using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Subscribe
{
    public class SubscribeListViewModel
    {
        public UserProfileDetailsViewModel User { get; set; }
        public SubscribeListItemViewModel[] Items { get; set; }

        public string[] PaymentTypeNames { get; set; }

        public bool AllowChanges { get; set; }

        public int ProjectId { get; set; }
    }

    public interface ISubscribeTarget : ILinkable
    {
        public string Name { get; }
    }

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
                User = new UserProfileDetailsViewModel(data.User, currentUser),
                AllowChanges = data.User == currentUser, //TODO allow project admins to setup subscribe for other masters
                Items = data.UserSubscriptions.Select(x => x.ToViewModel(uriService)).ToArray(),
                ProjectId = projectId,
                PaymentTypeNames = paymentTypes.Select(pt => pt.ToPaymentTypeName()).ToArray(),
            };
        }

        private static string ToPaymentTypeName(this PaymentTypeDto dto)
        {
            return dto.TypeKind switch
            {
                PaymentTypeKind.Custom => dto.Name,
                PaymentTypeKind.Cash => "Наличные",
                PaymentTypeKind.Online => "Онлайн",
                _ => throw new ArgumentOutOfRangeException("dto.TypeKind", (object) dto.TypeKind, "Wrong type"),
            };
        }

        private class AbstractLinkable : ISubscribeTarget
        {
            public LinkType LinkType { get; set; }

            public string Identification { get; set; }

            public int? ProjectId { get; set; }

            public string Name { get; set; }
        }

        public static SubscribeListItemViewModel ToViewModel(this UserSubscriptionDto dto, IUriService uriService)
        {
            var link = dto.ToSubscribeTargetLink();
            return new SubscribeListItemViewModel
            {
                Name = link.Name,
                Link = uriService.GetUri(link),
                UserSubscriptionId = dto.UserSubscriptionId,
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

            return new AbstractLinkable
            {
                LinkType = link.type,
                Identification = link.id.ToString(),
                ProjectId = dto.ProjectId,
                Name = link.name,
            };
        }

    }
}
