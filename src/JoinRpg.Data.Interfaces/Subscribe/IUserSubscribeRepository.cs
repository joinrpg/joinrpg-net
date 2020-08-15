using System.Threading.Tasks;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
    /// <summary>
    /// Load user subscribe settings 
    /// </summary>
    public interface IUserSubscribeRepository
    {
        /// <summary>
        /// Load every Subscriptions for project
        /// </summary>
        Task<(User User, UserSubscriptionDto[] UserSubscriptions)> LoadSubscriptionsForProject(int userId, int projectId);
    }
}
