using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;
using Joinrpg.Markdown;
using JoinRpg.Web.Filter;
using JoinRpg.Web.XGameApi.Contract.Schedule;
using JoinRpg.WebPortal.Managers.Schedule;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [RoutePrefix("x-game-api/{projectId}/schedule"), XGameMasterAuthorize()]
    public class ProjectScheduleController : XGameApiController
    {
        private SchedulePageManager Manager { get; }

        public ProjectScheduleController(IProjectRepository projectRepository, SchedulePageManager manager) : base(projectRepository)
        {
            Manager = manager;
        }

        [HttpGet]
        [Route("all")]
        public async Task<List<ProgramItemInfoApi>> GetSchedule([FromUri]
            int projectId)
        {
            var check = await Manager.CheckScheduleConfiguration();
            if (check.Any())
            {
                throw new Exception("Error");
            }

            var project = await ProjectRepository.GetProjectWithFieldsAsync(projectId);
            var characters = await ProjectRepository.GetCharacters(projectId);
            var scheduleBuilder = new ScheduleBuilder(project, characters);
            var result = scheduleBuilder.Build().AllItems.Select(slot =>
                new ProgramItemInfoApi
                {
                    Name = slot.ProgramItem.Name,
                    Authors = slot.ProgramItem.Authors.Select(author =>
                        new AuthorInfoApi {Name = author.GetDisplayName()}),
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Rooms = slot.Rooms.Select(room => new RoomInfoApi {Name = room.Name}),
                    Description = slot.ProgramItem.Description.ToHtmlString().ToString()
                }).ToList();
            return result;
        }
    }
}
