using System.Collections.Generic;

namespace JoinRpg.Web.GameSubscribe
{
    public class SubscribeListViewModel
    {
        public List<SubscribeListItemViewModel> Items { get; set; }

        public string[] PaymentTypeNames { get; set; }

        public bool AllowChanges { get; set; }

        public int ProjectId { get; set; }
        public int MasterId { get; set; }
    }
}
