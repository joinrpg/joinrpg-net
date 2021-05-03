using System;

namespace JoinRpg.Web.Models.Subscribe
{
    public class SubscribeListItemViewModel
    {
        public Uri Link { get; set; }
        public string Name { get; set; }

        public SubscribeOptionsViewModel Options { get; set; }

        public int UserSubscriptionId { get; set; }
        public int ProjectId { get; set; }
    }
}
