using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Models.Print;

namespace JoinRpg.WebPortal.Managers.Plots;

/// <summary>
/// Отчёт по раздаткам: что раздавать и сколько экземпляров печатать.
/// </summary>
public static class HandoutReportViewModelBuilder
{
    /// <summary>
    /// Считает количество экземпляров каждой раздатки, сохраняя порядок сюжетов.
    /// </summary>
    /// <param name="handoutsInPlotOrder">
    /// Раздатки в порядке сюжетов: папки — в заданном мастерами порядке, внутри папки — её элементы.
    /// Ровно так их отдаёт <see cref="IPlotRepository.GetPlotsBySpecification"/>.
    /// </param>
    /// <param name="charactersTargets">Таргеты персонажей, для которых строится отчёт.</param>
    /// <remarks>
    /// Агрегируем по списку раздаток, а не по персонажам: порядок обхода персонажей произволен, и
    /// отчёт получал строки в порядке первой встречи раздатки — то есть случайный.
    /// </remarks>
    public static HandoutReportViewModel Build(
        IReadOnlyCollection<PlotTextDto> handoutsInPlotOrder,
        IReadOnlyCollection<TargetsInfo> charactersTargets)
        => new([..
            handoutsInPlotOrder
                .Select(handout => (handout, count: charactersTargets.Count(target => handout.Target.HasIntersections(target))))
                .Where(x => x.count > 0)
                .Select(x => new HandoutReportItemViewModel(x.handout, x.count))]);
}
