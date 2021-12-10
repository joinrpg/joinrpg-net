using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.CheckIn;

namespace JoinRpg.Blazor.Client.ApiClients
{
    public class CheckInClient : ICheckInClient
    {
        private readonly HttpClient httpClient;

        public CheckInClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<CheckInStatViewModel> GetCheckInStats(ProjectIdentification projectId)
        {
            return await httpClient.GetFromJsonAsync<CheckInStatViewModel>(
                $"webapi/checkin/getstats?projectId={projectId.Value}")
                ?? throw new Exception("Couldn't get result from server");
        }
    }
}
