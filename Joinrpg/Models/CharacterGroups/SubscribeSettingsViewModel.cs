using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.CharacterGroups
{
    public class SubscribeSettingsViewModel : ISubscriptionOptions
    {
        public SubscribeSettingsViewModel()
        {
        }

        public SubscribeSettingsViewModel(User user, CharacterGroup group)
        {
            var direct =
                user.Subscriptions.SingleOrDefault(
                    s => s.CharacterGroupId == group.CharacterGroupId);
            Init(user, group, direct);
            ProjectId = group.ProjectId;
            CharacterGroupId = group.CharacterGroupId;
            Name = group.CharacterGroupName;
        }

        public string Name { get; }

        public SubscribeSettingsViewModel(User user, Character group)
        {
            var direct =
                user.Subscriptions.SingleOrDefault(s => s.CharacterId == group.CharacterId);
            Init(user, group, direct);
        }

        public SubscribeSettingsViewModel(User user, Claim claim)
        {
            var direct = user.Subscriptions.SingleOrDefault(s => s.ClaimId == claim.ClaimId);
            if (direct != null)
            {
                this.AssignFrom(direct);
            }
            else
            {
                var direct2 = user.Subscriptions.SingleOrDefault(s =>
                    s.CharacterId == claim.CharacterId ||
                    s.CharacterGroupId == claim.CharacterGroupId);
                Init(user, claim.GetTarget(), direct2);
            }
        }

        private void Init(User user, IWorldObject group, UserSubscription direct)
        {
            if (direct != null)
            {
                this.AssignFrom(direct);
            }

            foreach (var characterGroup in group.ParentGroups)
            {
                ParseCharacterGroup(characterGroup, user);
            }

        }

        private void ParseCharacterGroup(CharacterGroup characterGroup, User user)
        {
            var subscribe = user.Subscriptions.SingleOrDefault(s =>
                s.CharacterGroupId == characterGroup.CharacterGroupId);
            if (subscribe != null)
            {
                //Set what set in parent
                this.OrSetIn(subscribe);
                //Disable edit if set in parent
                EnabledFlags.AndNotSetIn(subscribe);

                if (!EnabledFlags.AnySet())
                {
                    return;
                }
            }

            foreach (var parentGroup in characterGroup.ParentGroups)
            {
                ParseCharacterGroup(parentGroup, user);
                if (!EnabledFlags.AnySet())
                {
                    return;
                }
            }
        }

        [Display(
            Name = "Подписка на новые заявки/прием/отклонение",
            Description = "Будут приходить уведомления о любых изменениях статуса заявки")]
        public bool ClaimStatusChange { get; set; }

        [Display(Name = "Подписка на комментарии", Description = "Будут приходить уведомления о любых комментариях к заявке")]
        public bool Comments { get; set; }

        [Display(Name = "Подписка на изменение полей персонажа/заявки")]
        public bool FieldChange { get; set; }

        [Display(Name = "Подписка на финансовые операции", Description = "Будут приходить уведомления о сданном взносе, его изменении и других финансовых операциях")]
        public bool MoneyOperation { get; set; }

        [Display(Name = "Подписка на операции с поселением", Description = "Будут приходить уведомления о изменении типа поселения, назначении комнаты и других операциях с поселением")]
        public bool AccommodationChange { get; set; }

        public ISubscriptionOptions EnabledFlags { get; } = SubscribeOptionsExtensions.AllSet();


        public int ProjectId { get; set; }
        public int CharacterGroupId { get; set; }

        public ISubscriptionOptions GetOptionsToSubscribeDirectly() => this.AndSetIn(EnabledFlags);


    }
}
