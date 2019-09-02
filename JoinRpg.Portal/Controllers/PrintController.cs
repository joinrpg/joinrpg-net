using System;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Print;
using Microsoft.AspNetCore.Mvc;
using ActionResult = Microsoft.AspNetCore.Mvc.ActionResult;

namespace JoinRpg.Portal.Controllers
{
  [Microsoft.AspNetCore.Authorization.Authorize]
  public class PrintController : Common.ControllerGameBase
    {
        private IPlotRepository PlotRepository { get; }
    private IPluginFactory PluginFactory { get; }
    private ICharacterRepository CharacterRepository { get; }
      private IUriService UriService { get; }

      public PrintController(
          IProjectRepository projectRepository,
          IProjectService projectService,
          IPlotRepository plotRepository,
          IPluginFactory pluginFactory,
          ICharacterRepository characterRepository,
          IUriService uriService,
          IUserRepository userRepository) : base(projectRepository, projectService, userRepository)
        {
            PlotRepository = plotRepository;
      PluginFactory = pluginFactory;
      CharacterRepository = characterRepository;
        UriService = uriService;
    }


    public async Task<IActionResult> Character(int projectid, int characterid)
    {
      var character = await CharacterRepository.GetCharacterWithGroups(projectid, characterid);
      var error = WithCharacter(character);
      if (error != null) return error;

      return View(new PrintCharacterViewModel(CurrentUserId, character, await PlotRepository.GetPlotsForCharacter(character), UriService));
    }

    [MasterAuthorize()]
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
    public async Task<ActionResult> Index(int projectId)
    {
      var characters = (await ProjectRepository.GetCharacters(projectId)).Where(c => c.IsActive).ToList();

      var project = await GetProjectFromList(projectId, characters);

      var pluginNames =
        PluginFactory.GetProjectOperations<IPrintCardPluginOperation>(project).Select(
          PluginOperationDescriptionViewModel.Create);

      return
        View(new PrintIndexViewModel(projectId,
          characters.Select(c => c.CharacterId).ToArray(), pluginNames));
    }

    [MasterAuthorize()]
    public async Task<ActionResult> HandoutReport(int projectid)
    {
      var plotElements =
        await PlotRepository.GetActiveHandouts(projectid);

      var characters = (await ProjectRepository.GetCharacters(projectid)).Where(c => c.IsActive).ToList();

      return View(new HandoutReportViewModel(plotElements, characters));
    }

    public async Task<IActionResult> PrintCards(int projectid, string plugin, string characterIds)
    {
      var characters = await CharacterRepository.GetCharacters(projectid, characterIds.UnCompressIdList());
      var project = await GetProjectFromList(projectid, characters);
      var pluginInstance = PluginFactory.GetOperationInstance<IPrintCardPluginOperation>(project, plugin);
      if (pluginInstance == null)
      {
        return HttpNotFound();
      }

      //TODO display correct errors
      if (!pluginInstance.AllowPlayerAccess)
      {
                if (!project.HasMasterAccess(CurrentUserId))
                {
                    return Unauthorized();
                }

      }
      else
      {
        
        if (characters.Any(c => !c.HasAnyAccess(CurrentUserId)))
        {
            return Unauthorized();
        }
      }

      var cards = characters.SelectMany(c => PluginFactory.PrintForCharacter(pluginInstance, c));

      return View(cards);
    }

    [MasterAuthorize()]
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

    private static string GetFeeDueString(PrintCharacterViewModelSlim v)
    {
      return v.FeeDue == 0 ? "" : $"<div style='background-color:lightgray; text-align:center'><b>Взнос</b>: {v.FeeDue}₽ </div>";
    }

        [Obsolete]
        protected IActionResult WithCharacter(Character character)
        {
            if (character == null)
            {
                return HttpNotFound();
            }
            if (!character.HasAnyAccess(CurrentUserId))
            {
                return NoAccesToProjectView(character.Project);
            }

            return null;
        }
    }
}
