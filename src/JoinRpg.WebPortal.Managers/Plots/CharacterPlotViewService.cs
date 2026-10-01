using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Interfaces;

namespace JoinRpg.WebPortal.Managers.Plots;

//TODO: extract interface
public class CharacterPlotViewService(
    ICharacterInfoRepository characterInfoRepository,
    IPlotRepository plotRepository,
    ICurrentUserAccessor currentUser
    )
{
    public async Task<IReadOnlyDictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>> GetHandoutsForActiveCharacters(ProjectIdentification projectId, PlotVersionFilter version)
    {
        var plotInfo = await LoadPlotInfoForActiveCharacters(projectId, CharacterAccessMode.Print);

        var specification = new PlotSpecification(plotInfo.Values.Select(x => x.Targets).UnionAll(), version, PlotElementType.Handout);

        var plots = await plotRepository.GetPlotsBySpecification(specification);

        return MapPlotToTargets(plotInfo, plots);
    }

    public async Task<IReadOnlyDictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>> GetHandoutsForCharacters(
        IReadOnlyCollection<CharacterIdentification> characterIdList)
    {
        characterIdList.EnsureSameProject();

        if (characterIdList.Count == 0)
        {
            return new Dictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>();
        }

        var plotInfo = await LoadPlotInfoForCharacters(characterIdList, CharacterAccessMode.Print);

        var specification = new PlotSpecification(plotInfo.Values.Select(x => x.Targets).UnionAll(), PlotVersionFilter.PublishedVersion, PlotElementType.Handout);

        var plots = await plotRepository.GetPlotsBySpecification(specification);

        return MapPlotToTargets(plotInfo, plots);
    }

    public async Task<IReadOnlyDictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>> GetPlotForCharacters(
        IReadOnlyCollection<CharacterIdentification> characterIdList,
        CharacterAccessMode characterAccessMode)
    {
        characterIdList.EnsureSameProject();

        if (characterIdList.Count == 0)
        {
            return new Dictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>();
        }
        var plotInfo = await LoadPlotInfoForCharacters(characterIdList, characterAccessMode);

        var specification = new PlotSpecification(plotInfo.Values.Select(x => x.Targets).UnionAll(), PlotVersionFilter.PublishedVersion, PlotElementType.RegularPlot);

        var plots = await plotRepository.GetPlotsBySpecification(specification);

        return MapPlotToTargets(plotInfo, plots);
    }

    private static Dictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>> MapPlotToTargets(Dictionary<CharacterIdentification, ChPlotInfo> targets, IReadOnlyCollection<PlotTextDto> plots)
    {
        var dict = new Dictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>(targets.Count);

        foreach (var (characterId, info) in targets)
        {
            List<PlotTextDto> characterPlots = [];

            foreach (var plot in plots)
            {
                TargetsInfo plotTarget = plot.Target;
                if (plotTarget.HasIntersections(info.Targets))
                {
                    characterPlots.Add(plot);
                }
            }

            dict[characterId] = [.. characterPlots.OrderByStoredOrder(info.Ordering, preserveOrder: true)];
        }

        return dict;
    }

    private async Task<Dictionary<CharacterIdentification, ChPlotInfo>> LoadPlotInfoForCharacters(IReadOnlyCollection<CharacterIdentification> characterIdList, CharacterAccessMode characterAccessMode)
    {
        var characters = await characterInfoRepository.GetCharacterInfos(characterIdList);

        return ToPlotInfo(characters, characterAccessMode);
    }

    private async Task<Dictionary<CharacterIdentification, ChPlotInfo>> LoadPlotInfoForActiveCharacters(ProjectIdentification projectId, CharacterAccessMode characterAccessMode)
    {
        var characters = await characterInfoRepository.GetAllCharacterInfos(projectId, CharacterStatusSpec.Active);

        return ToPlotInfo(characters, characterAccessMode);
    }

    private Dictionary<CharacterIdentification, ChPlotInfo> ToPlotInfo(
        IReadOnlyCollection<CharacterInfo> characters,
        CharacterAccessMode characterAccessMode)
        => characters
            .Where(c => AccessArgumentsFactory.Create(c, currentUser, characterAccessMode).CharacterPlotAccess)
            // Порядок вставки в словарь виден наружу: HandoutReport обходит его как есть,
            // и строки отчёта должны идти по именам персонажей. Раньше за это отвечал
            // ORDER BY в GetAllCharacters, теперь сортируем явно.
            .OrderBy(c => c.CharacterName)
            .ToDictionary(x => x.Id, x => new ChPlotInfo(new TargetsInfo(x), x.PlotElementOrderData));

    private record ChPlotInfo(TargetsInfo Targets, string? Ordering);

    public async Task<IReadOnlyList<PlotTextDto>> GetPlotsForCharacter(CharacterIdentification characterId)
    {
        var dict = await GetPlotForCharacters([characterId], CharacterAccessMode.Usual);
        return dict.TryGetValue(characterId, out var plot) ? plot : [];
    }
}
