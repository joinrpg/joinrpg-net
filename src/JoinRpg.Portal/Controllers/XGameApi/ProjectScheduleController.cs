using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Schedules;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.Web.Schedule;
using JoinRpg.WebPortal.Managers.Schedule;
using JoinRpg.XGameApi.Contract.Schedule;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/schedule")]
public class ProjectScheduleController(IProjectRepository projectRepository, SchedulePageManager manager) : XGameApiController
{
    [HttpGet]
    [Route("all")]
    [ProducesResponseType(410)]
    [ProducesResponseType(403)]
    [ProducesResponseType(200)]
    public async Task<ActionResult<List<ProgramItemInfoApi>>> GetSchedule([FromRoute]
        int projectId)
    {
        var project = await projectRepository.GetProjectWithFieldsAsync(projectId);

        if (project is null)
        {
            return Problem(statusCode: 410);
        }

        var check = await manager.CheckScheduleConfiguration();
        if (check.Contains(ScheduleConfigProblemsViewModel.NoAccess))
        {
            return Forbid();
        }
        if (check.Any())
        {
            return Problem(detail: check.Select(x => x.ToString()).JoinStrings(" ,"), statusCode: 400);
        }

        var scheduleBuilder = await manager.GetBuilder();

        return scheduleBuilder.Build().AllItems.Select(ToProgramItemInfoApi).ToList();
    }

    private ProgramItemInfoApi ToProgramItemInfoApi(ProgramItemPlaced slot)
    {
        return new ProgramItemInfoApi
        {
            ProgramItemId = slot.ProgramItem.Id.CharacterId,
            Name = slot.ProgramItem.Name,
            Authors = slot.ProgramItem.Authors.Select(author =>
                            new AuthorInfoApi
                            {
                                UserId = author.UserId,
                                Name = author.DisplayName.DisplayName,
                            }),
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            Rooms = slot.Rooms.Distinct().Select(room => new RoomInfoApi
            {
                RoomId = room.Id.ProjectFieldVariantId,
                Name = room.Name,
            }),
            Description = slot.ProgramItem.Description.ToPlainTextWithoutHtmlEscape().ToString(),
            DescriptionHtml = slot.ProgramItem.Description.ToHtmlString().ToString(),
            DescriptionMarkdown = slot.ProgramItem.Description.Value,
            ProgramItemDetailsUri = new Uri(GetProgramItemLink(slot)),
            ProjectId = slot.ProgramItem.Id.ProjectId,
        };

        string GetProgramItemLink(ProgramItemPlaced slot)
        {
            return Url.ActionLink("Details", "Character",
                    new { ProjectId = (int)slot.ProgramItem.Id.ProjectId, slot.ProgramItem.Id.CharacterId })
                ?? throw new InvalidOperationException("URI should be present");
        }
    }
}
