using System.Text.Json;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.Acl;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Acl;

public class MasterMoveItemViewModelTest
{
    private static readonly MasterMoveItemViewModel Item = new(
        new ProjectMasterIdentification(new ProjectIdentification(5), 7), "Мастер Вася", "Мастер по боёвке");

    [Fact]
    public void RoundTripsThroughJson_AsIslandParameter()
    {
        // Модель ездит в остров MastersMoveControl по JSON: явные члены IMoveableListItem в неё не попадают.
        var json = JsonSerializer.Serialize(Item);

        JsonSerializer.Deserialize<MasterMoveItemViewModel>(json).ShouldBe(Item);
        using var document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(["MasterId", "DisplayName", "Role"]);
    }

    [Fact]
    public void IsMoveableItem_InsideItsProject()
    {
        IMoveableListItem moveable = Item;

        moveable.Id.ShouldBe(Item.MasterId.ToString());
        moveable.ParentId.ShouldBe(new ProjectIdentification(5).ToString());
        moveable.DisplayText.ShouldBe("Мастер Вася");
        moveable.Subtext.ShouldBe("Мастер по боёвке");
    }
}
