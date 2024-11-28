using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Schedules;
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
            ProgramItemId = slot.ProgramItem.Id,
            Name = slot.ProgramItem.Name,
            Authors = slot.ProgramItem.Authors.Select(author =>
                            new AuthorInfoApi
                            {
                                UserId = author.UserId,
                                Name = author.GetDisplayName(),
                            }),
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            Rooms = slot.Rooms.Distinct().Select(room => new RoomInfoApi
            {
                RoomId = room.Id.ProjectFieldVariantId,
                Name = room.Name,
            }),
            Description = slot.ProgramItem.Description.ToPlainText().ToString(),
            DescriptionHtml = slot.ProgramItem.Description.ToHtmlString().ToString(),
            DescriptionMarkdown = slot.ProgramItem.Description.Contents,
            ProgramItemDetailsUri = new Uri(GetProgramItemLink(slot)),
            ProjectId = slot.ProgramItem.ProjectId,
        };

        string GetProgramItemLink(ProgramItemPlaced slot)
        {
            return Url.ActionLink("Details", "Character", new { slot.ProgramItem.ProjectId, CharacterId = slot.ProgramItem.Id })
                ?? throw new InvalidOperationException("URI should be present");
        }
    }
}
