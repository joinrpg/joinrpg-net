using System.Threading.Tasks;
using JoinRpg.Web.CheckIn;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi
{
    [Route("/webapi/checkin/[action]")]
    public class CheckInController : ControllerBase
    {
        private readonly ICheckInClient checkInClient;

        public CheckInController(ICheckInClient checkInClient) => this.checkInClient = checkInClient;

        [HttpGet]
        public async Task<CheckInStatViewModel> GetStats(int projectId)
            => await checkInClient.GetCheckInStats(new PrimitiveTypes.ProjectIdentification(projectId));
    }
}
