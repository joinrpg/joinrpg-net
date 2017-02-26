using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.DataModel
{
    public class UserSubscriptionTooltip
    {
        public string Tooltip { get; set; }
        public bool HasFullParentSubscription { get; set; }
        public bool IsDirect { get; set; }
    }
}
