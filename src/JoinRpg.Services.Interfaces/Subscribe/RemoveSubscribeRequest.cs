using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces.Subscribe
{
    public record RemoveSubscribeRequest(int ProjectId, int UserSubscribtionId)
    {
    }
}
