using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.Print;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Authorize]
    [Route("{projectId}/print/[action]")]
    public class PrintController : Common.ControllerGameBase
    {
        private IPlotRepository PlotRepository { get; }
        private ICharacterRepository CharacterRepository { get; }
        private IUriService UriService { get; }

        public PrintController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IPlotRepository plotRepository,
            ICharacterRepository characterRepository,
            IUriService uriService,
            IUserRepository userRepository) : base(projectRepository, projectService, userRepository)
        {
            PlotRepository = plotRepository;
            CharacterRepository = characterRepository;
            UriService = uriService;
        }


        [HttpGet]
        public async Task<IActionResult> Character(int projectid, int characterid)
        {
            var character = await CharacterRepository.GetCharacterWithGroups(projectid, characterid);
            if (character == null)
            {
                return NotFound();
            }
            if (!character.HasAnyAccess(CurrentUserId))
            {
                return NoAccesToProjectView(character.Project);
            }

            return View(new PrintCharacterViewModel(CurrentUserId, character, await PlotRepository.GetPlotsForCharacter(character), UriService));
        }

        [MasterAuthorize()]
        [HttpGet]
        public async Task<ActionResult> CharacterList(int projectid, string characterIds)
        {
            var characters = await ProjectRepository.LoadCharactersWithGroups(projectid, characterIds.UnCompressIdList());

            var plotElements = (await PlotRepository.GetPlotsWithTargetAndText(projectid)).SelectMany(p => p.Elements).ToArray();

            var viewModel =
              characters.Select(
                c => new PrintCharacterViewModel(CurrentUserId, c, plotElements, UriService)).ToArray();

            return View(viewModel);
        }

        [MasterAuthorize()]
        [HttpGet]
        public async Task<ActionResult> Index(int projectId)
        {
            var characters = (await ProjectRepository.GetCharacters(projectId)).Where(c => c.IsActive).ToList();

            return
              View(new PrintIndexViewModel(projectId, characters.Select(c => c.CharacterId).ToArray()));
        }

        [MasterAuthorize()]
        [HttpGet]
        public async Task<ActionResult> HandoutReport(int projectid)
        {
            var plotElements =
              await PlotRepository.GetActiveHandouts(projectid);

            var characters = (await ProjectRepository.GetCharacters(projectid)).Where(c => c.IsActive).ToList();

            return View(new HandoutReportViewModel(plotElements, characters));
        }

        [MasterAuthorize()]
        [HttpGet]
        public async Task<ActionResult> Envelopes(int projectid, string characterids)
        {
            var characters = await ProjectRepository.LoadCharactersWithGroups(projectid, characterids.UnCompressIdList());

            var viewModel =
              characters.Select(
                c => new PrintCharacterViewModelSlim(c)).ToArray();

            var cards = viewModel.Select(v => new HtmlCardPrintResult($@"
{GetFeeDueString(v)}
<b>Игрок</b>: {v.PlayerDisplayName ?? "нет"}<br>
<b>ФИО</b>: {v.PlayerFullName ?? "нет"}<br>
<hr>
<b>Персонаж</b>: {v.CharacterName ?? "нет"}<br>
<b>Мастер</b>: {v.ResponsibleMaster?.GetDisplayName() ?? "нет"}<br>
<hr>
<i>{v.Groups.Select(g => g.Name).JoinStrings(" • ")}</i><br>
", CardSize.A7));
            ViewBag.CardSanitizeDisable = true;
            return View("PrintCards", cards);
        }

        private static string GetFeeDueString(PrintCharacterViewModelSlim v) => v.FeeDue == 0 ? "" : $"<div style='background-color:lightgray; text-align:center'><b>Взнос</b>: {v.FeeDue}₽ </div>";
    }
}
