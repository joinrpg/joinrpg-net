using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi;

[Route("x-api/antimspam")]
public class AntiSpamServiceController : XGameApiController
{
    internal IAntiSpamService Service { get; }

    /// <inheritdoc />
    public AntiSpamServiceController(IProjectRepository projectRepository, IAntiSpamService service) : base(projectRepository) => Service = service;

    [Route("islarper")]
    [HttpGet]
    public Task<bool> IsLarper(IsLarperRequestModel model)
    {
        return Service.IsLarper(new IsLarperRequest()
        {
            VkNickName = model.VkNickName,
            VkId = model.VkId,
            Email = model.Email,
            TelegramNickName = model.TelegramNickName
        });
    }
}
