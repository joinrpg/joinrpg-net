﻿using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models.Print;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class PrintController : ControllerGameBase
  {
    private IPlotRepository PlotRepository { get; }
    private IPluginFactory PluginFactory { get; }

    public PrintController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IPlotRepository plotRepository,
      IPluginFactory pluginFactory) : base(userManager, projectRepository, projectService, exportDataService)
    {
      PlotRepository = plotRepository;
      PluginFactory = pluginFactory;
    }


    public async Task<ActionResult> Character(int projectid, int characterid)
    {
      var character = await ProjectRepository.GetCharacterWithGroups(projectid, characterid);
      var error = WithCharacter(character);
      if (error != null) return error;

      return View(new PrintCharacterViewModel(CurrentUserId, character, await PlotRepository.GetPlotsForCharacter(character)));
    }

    public async Task<ActionResult> CharacterList(int projectid, string characterIds)
    {
      var characters = await ProjectRepository.LoadCharactersWithGroups(projectid, characterIds.UnCompressIdList());

      var error = await AsMaster(characters, projectid);
      if (error != null) return error;

      var plotElements = (await PlotRepository.GetPlotsWithTargetAndText(projectid)).SelectMany(p => p.Elements).ToArray();

      var viewModel =
        characters.Select(
          c => new PrintCharacterViewModel(CurrentUserId, c, plotElements)).ToArray();

      return View(viewModel);
    }

    public async Task<ActionResult> Index(int projectId)
    {
      var characters = (await ProjectRepository.GetCharacters(projectId)).Where(c => c.IsActive).ToList();
      var error = await AsMaster(characters, projectId);
      if (error != null) return error;

      var pluginNames =
        (await PluginFactory.GetPossibleOperations<IPrintCardPluginOperation>(projectId)).Select(
          PluginOperationDescriptionViewModel.Create);

      //TODO: That doesn't belong here
      var configPluginNames =
  (await PluginFactory.GetPossibleOperations<IShowConfigurationPluginOperation>(projectId)).Select(
    PluginOperationDescriptionViewModel.Create);

      return
        View(new PrintIndexViewModel(projectId,
          characters.Select(c => c.CharacterId).ToArray(), pluginNames, configPluginNames));
    }

    public async Task<ActionResult> HandoutReport(int projectid)
    {
      var plotElements =
        await PlotRepository.GetActiveHandouts(projectid);

      var characters = (await ProjectRepository.GetCharacters(projectid)).Where(c => c.IsActive).ToList();
      var error = await AsMaster(characters, projectid);
      if (error != null) return error;

      return View(new HandoutReportViewModel(plotElements, characters));
    }

    public async Task<ActionResult> PrintCards(int projectid, string plugin, string characterIds)
    {
      var pluginInstance = await PluginFactory.GetOperationInstance<IPrintCardPluginOperation>(projectid, plugin);
      if (pluginInstance == null)
      {
        return HttpNotFound();
      }
      var characters = await ProjectRepository.LoadCharacters(projectid, characterIds.UnCompressIdList());

      if (!pluginInstance.AllowPlayerAccess)
      {
        var error = await AsMaster(characters, projectid);
        if (error != null) return error;
      }
      else
      {
        //TODO display correct errors
        if (characters.Any(c => !c.HasAnyAccess(CurrentUserId)))
        {
          return new  HttpUnauthorizedResult();
        }
      }

      var cards = characters.SelectMany(c => PluginFactory.PrintForCharacter(pluginInstance, c));

      return View(cards);
    }

    public async Task<ActionResult> Envelopes(int projectid, string characterids)
    {
      var characters = await ProjectRepository.LoadCharactersWithGroups(projectid, characterids.UnCompressIdList());

      var error = await AsMaster(characters, projectid);
      if (error != null) return error;

      var viewModel =
        characters.Select(
          c => new PrintCharacterViewModelSlim(c)).ToArray();

      var cards = viewModel.Select(v => new HtmlCardPrintResult($@"
{GetFeeDueString(v)}
<b>Игрок</b>: {v.PlayerDisplayName ?? "нет"}<br>
<b>ФИО</b>: {v.PlayerFullName ?? "нет"}<br>
<hr>
<b>Персонаж</b>: {v.CharacterName ?? "нет"}<br>
<b>Мастер</b>: {v.ResponsibleMaster?.DisplayName ?? "нет"}<br>
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
  }
}