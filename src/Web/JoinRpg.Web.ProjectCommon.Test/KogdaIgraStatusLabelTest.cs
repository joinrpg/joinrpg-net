using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon.KogdaIgra;

namespace JoinRpg.Web.ProjectCommon.Test;

public class KogdaIgraStatusLabelTest
{
    [Theory]
    [InlineData(null)]
    [InlineData(KogdaIgraGameStatus.Ok)]
    [InlineData(KogdaIgraGameStatus.Passed)]
    public void NormalStatuses_HaveNoWarningLabel(KogdaIgraGameStatus? status)
    {
        status.ToWarningLabel().ShouldBeNull();
    }

    [Theory]
    [InlineData(KogdaIgraGameStatus.Unknown, "Статус неизвестен")]
    [InlineData(KogdaIgraGameStatus.Postponed, "Отложена")]
    [InlineData(KogdaIgraGameStatus.DateUncertain, "Дата уточняется")]
    [InlineData(KogdaIgraGameStatus.Cancelled, "Отменена")]
    public void WarningStatuses_HaveLabel(KogdaIgraGameStatus status, string expected)
    {
        ((KogdaIgraGameStatus?)status).ToWarningLabel().ShouldBe(expected);
    }

    [Fact]
    public void CodeUnknownToUs_HasRawCodeLabel()
    {
        ((KogdaIgraGameStatus?)42).ToWarningLabel().ShouldBe("Статус 42");
    }

    [Theory]
    [InlineData(2026, KogdaIgraGameStatus.Cancelled, "2026 · Отменена")]
    [InlineData(2026, KogdaIgraGameStatus.Ok, "2026")]
    [InlineData(2026, null, "2026")]
    [InlineData(null, KogdaIgraGameStatus.Cancelled, "Отменена")]
    [InlineData(null, null, "")]
    public void Subtext_CombinesYearAndWarningLabel(int? year, KogdaIgraGameStatus? status, string expected)
    {
        var game = new KogdaIgraShortViewModel(new KogdaIgraIdentification(1), "Игра", new Uri("https://kogda-igra.ru/game/1"), year, status);
        game.ToSelectorSubtext().ShouldBe(expected);
    }
}
