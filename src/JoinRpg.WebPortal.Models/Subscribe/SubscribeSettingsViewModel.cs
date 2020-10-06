using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Subscribe
{
    public class SubscribeSettingsViewModel
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
                Options.AssignFrom(direct);
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
                Options.AssignFrom(direct);
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
                Options.OrSetIn(subscribe);
                //Disable edit if set in parent
                DisabledFlags.OrSetIn(subscribe);

                if (DisabledFlags.AllSet)
                {
                    return;
                }
            }

            foreach (var parentGroup in characterGroup.ParentGroups)
            {
                ParseCharacterGroup(parentGroup, user);
                if (DisabledFlags.AllSet)
                {
                    return;
                }
            }
        }

        public SubscribeOptionsViewModel Options { get; set; } = new SubscribeOptionsViewModel();

        public SubscribeOptionsViewModel DisabledFlags { get; set; } = new SubscribeOptionsViewModel();


        public int ProjectId { get; set; }
        public int CharacterGroupId { get; set; }

        public ISubscriptionOptions GetOptionsToSubscribeDirectly() => Options.AndNotSetIn(DisabledFlags);


    }
}
