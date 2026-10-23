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
            new BadgeLine(ProjectFieldId: 7, StaticText: null, SlotLabel: null, OffsetMm: 10, MinFontPt: 12, MaxFontPt: 32, Bold: true),
            new BadgeLine(ProjectFieldId: null, StaticText: "Каэр Морхен", SlotLabel: null, OffsetMm: 40, MinFontPt: 8, MaxFontPt: 12, Bold: false),
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

    // У строки с полем текст сохраняется — это пример для превью.
    [Fact]
    public void ToLayout_KeepsSampleTextWithField()
    {
        var model = EditBadgeTemplateViewModel.FromDomain(template);
        model.Lines[1].FieldId = new ProjectFieldIdentification(new ProjectIdentification(5), 8);

        var line = model.ToLayout().Lines[1];

        line.ProjectFieldId.ShouldBe(8);
        line.StaticText.ShouldBe("Каэр Морхен");
    }

    [Fact]
    public void ShouldRoundTripBottomAnchor()
    {
        var bottom = template with
        {
            Layout = template.Layout with { Lines = [template.Layout.Lines[0] with { Anchor = BadgeLineAnchor.Bottom }] },
        };

        EditBadgeTemplateViewModel.FromDomain(bottom).ToLayout().Lines.ShouldBe(bottom.Layout.Lines);
    }

    // Новый шаблон сразу с подписью сайта у нижнего края: 5 мм от низа, шрифт 6 пт.
    [Fact]
    public void NewTemplate_HasSiteCreditAtBottom()
    {
        var line = EditBadgeTemplateViewModel.CreateNew().ToLayout().Lines.ShouldHaveSingleItem();

        line.StaticText.ShouldBe("при помощи joinrpg.ru");
        line.ProjectFieldId.ShouldBeNull();
        line.Anchor.ShouldBe(BadgeLineAnchor.Bottom);
        line.OffsetMm.ShouldBe(5);
        (line.MinFontPt, line.MaxFontPt).ShouldBe((6, 6));
    }
}
