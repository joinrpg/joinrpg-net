using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using JoinRpg.Web.GameSubscribe;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace JoinRpg.Blazor.Client.ApiClients
{
    public class GameSubscribeClient : IGameSubscribeClient
    {
        private readonly HttpClient httpClient;
        private readonly IJSRuntime jsRuntime;
        private readonly ILogger<GameSubscribeClient> logger;
        private readonly Lazy<Task<string?>> CsrfToken;

        public GameSubscribeClient(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<GameSubscribeClient> logger)
        {
            this.httpClient = httpClient;
            this.jsRuntime = jsRuntime;
            this.logger = logger;
            CsrfToken = new Lazy<Task<string?>>(GetCsrfTokenAsync);
        }

        private class StringHolder { public string Content { get; set; } }

        private async Task<string?> GetCsrfTokenAsync()
        {
            var cookies = await jsRuntime.InvokeAsync<StringHolder>("joinmethods.GetDocumentCookie");
            return cookies
                .Content
                .Split(';')
                .Select(v => v.TrimStart().Split('='))
                .Where(s => s[0] == "CSRF-TOKEN")
                .Select(s => s[1])
                .FirstOrDefault();
        }

        public async Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId)
        {
            return await httpClient.GetFromJsonAsync<SubscribeListViewModel>(
                $"webapi/gamesubscribe/getformaster?projectId={projectId}&masterId={masterId}")
                ?? throw new Exception("Couldn't get result from server");
        }

        public async Task RemoveSubscription(int projectId, int userSubscriptionsId)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await CsrfToken.Value);
                await httpClient.PostAsync($"webapi/gamesubscribe/unsubscribe?projectId={projectId}&userSubscriptionsId={userSubscriptionsId}", null);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during access");
                throw;
            }
        }
    }
}
