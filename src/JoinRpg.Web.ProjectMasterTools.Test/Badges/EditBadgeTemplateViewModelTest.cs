using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Web.ProjectMasterTools.Badges;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Badges;

public class EditBadgeTemplateViewModelTest
{
    private static readonly ProjectBadgeTemplate template = new(
        new ProjectBadgeTemplateIdentification(new ProjectIdentification(5), 3),
        "Игрок",
        new BadgeLayout(90, 55, new Uri("https://example.com/bg.png"),
        [
            new BadgeLine(ProjectFieldId: 7, StaticText: null, SlotLabel: null, TopMm: 10, MinFontPt: 12, MaxFontPt: 32, Bold: true),
            new BadgeLine(ProjectFieldId: null, StaticText: "Каэр Морхен", SlotLabel: null, TopMm: 40, MinFontPt: 8, MaxFontPt: 12, Bold: false),
        ]),
        IsActive: true);

    [Fact]
    public void ShouldRoundTripBackground()
        => EditBadgeTemplateViewModel.FromDomain(template).ToLayout().BackgroundUrl.ShouldBe(template.Layout.BackgroundUrl);

    // Без отдельной ошибки неразборчивая ссылка молча превратилась бы в «без фона».
    [Fact]
    public void UnparsableBackground_IsValidationError()
    {
        var model = EditBadgeTemplateViewModel.FromDomain(template);
        model.BackgroundUrl = "картинка.png";

        model.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(model)).ShouldNotBeEmpty();
    }

    [Fact]
    public void ShouldRoundTripLayout()
        => EditBadgeTemplateViewModel.FromDomain(template).ToLayout().Lines.ShouldBe(template.Layout.Lines);

    // Переключили строку с текста на поле — старый текст не должен уехать на сервер вторым источником.
    [Fact]
    public void ToLayout_TakesOnlySelectedSource()
    {
        var model = EditBadgeTemplateViewModel.FromDomain(template);
        model.Lines[1].Source = BadgeLineSource.Field;
        model.Lines[1].FieldId = new ProjectFieldIdentification(new ProjectIdentification(5), 8);

        var line = model.ToLayout().Lines[1];

        line.ProjectFieldId.ShouldBe(8);
        line.StaticText.ShouldBeNull();
    }
}
