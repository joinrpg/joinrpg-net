using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using JoinRpg.Web.GameSubscribe;

namespace JoinRpg.Blazor.Client.ApiClients
{
    public class GameSubscribeClient : IGameSubscribeClient
    {
        private readonly HttpClient httpClient;

        public GameSubscribeClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId)
        {
            return await httpClient.GetFromJsonAsync<SubscribeListViewModel>(
                $"webapi/gamesubscribe/getformaster?projectId={projectId}&masterId={masterId}")
                ?? throw new Exception("Couldn't get result from server");
        }
    }
}
