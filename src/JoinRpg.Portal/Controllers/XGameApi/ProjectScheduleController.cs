using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Schedules;
using JoinRpg.Web.XGameApi.Contract.Schedule;
using JoinRpg.WebPortal.Managers.Schedule;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [Route("x-game-api/{projectId}/schedule")]
    public class ProjectScheduleController : XGameApiController
    {
        private SchedulePageManager Manager { get; }

        public ProjectScheduleController(IProjectRepository projectRepository, SchedulePageManager manager) : base(projectRepository) => Manager = manager;

        [HttpGet]
        [Route("all")]
        [ProducesResponseType(410)]
        [ProducesResponseType(403)]
        [ProducesResponseType(200)]
        public async Task<ActionResult<List<ProgramItemInfoApi>>> GetSchedule([FromRoute]
            int projectId)
        {
            var project = await ProjectRepository.GetProjectWithFieldsAsync(projectId);

            if (project is null)
            {
                return Problem(statusCode: 410);
            }

            var check = await Manager.CheckScheduleConfiguration();
            if (check.Contains(ScheduleConfigProblemsViewModel.NoAccess))
            {
                return Forbid();
            }
            if (check.Any())
            {
                throw new Exception($"Error {check.Select(x => x.ToString()).JoinStrings(" ,")}");
            }

            var characters = await ProjectRepository.GetCharacters(projectId);

            var scheduleBuilder = new ScheduleBuilder(project, characters);
            var result = scheduleBuilder.Build().AllItems.Select(slot =>
                new ProgramItemInfoApi
                {
                    ProgramItemId = slot.ProgramItem.Id,
                    Name = slot.ProgramItem.Name,
                    Authors = slot.ProgramItem.Authors.Select(author =>
                        new AuthorInfoApi
                        {
                            UserId = author.UserId,
                            Name = author.GetDisplayName()
                        }),
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Rooms = slot.Rooms.Select(room => new RoomInfoApi
                    {
                        RoomId = room.Id,
                        Name = room.Name
                    }),
                    Description = slot.ProgramItem.Description.ToPlainText().ToString(),
                    DescriptionHtml = slot.ProgramItem.Description.ToHtmlString().ToString(),
                    DescriptionMarkdown = slot.ProgramItem.Description.Contents,
                }).ToList();
            return result;
        }
    }
}
