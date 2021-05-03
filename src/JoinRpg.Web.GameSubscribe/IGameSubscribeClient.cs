using System.Threading.Tasks;

namespace JoinRpg.Web.GameSubscribe
{
    public interface IGameSubscribeClient
    {
        Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId);
    }
}
