using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Web.ProjectCommon.ElementMoving;

namespace JoinRpg.WebPortal.Managers.Test.Fields;

public class MoveRequestTryParseTests
{
    [Theory]
    [InlineData("ProjectFieldId(1-2)", "Project(1)", null)]
    [InlineData("ProjectFieldId(1-2)", "Project(1)", "ProjectFieldId(1-3)")]
    [InlineData("ProjectFieldId(1-2)", "Project(1)", "ProjectFieldId(1-2)")]
    public void TryParse_ValidFieldMove_ReturnsTrue(string selfId, string parentId, string? moveAfterId)
    {
        MoveRequest.TryParse(selfId, parentId, moveAfterId, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.SelfId.ShouldBeOfType<ProjectFieldIdentification>();
        result.ParentId.ShouldBeOfType<ProjectIdentification>();
        if (moveAfterId is not null)
        {
            result.MoveAfterId.ShouldBeOfType<ProjectFieldIdentification>();
        }
        else
        {
            result.MoveAfterId.ShouldBeNull();
        }
    }

    /// <summary>
    /// Перестановка сюжетов в списке сюжетов проекта (#238): parent — проект.
    /// </summary>
    [Theory]
    [InlineData("PlotFolderId(1-2)", "Project(1)", null)]
    [InlineData("PlotFolderId(1-2)", "Project(1)", "PlotFolderId(1-3)")]
    public void TryParse_ValidPlotFolderMove_ReturnsTrue(string selfId, string parentId, string? moveAfterId)
    {
        MoveRequest.TryParse(selfId, parentId, moveAfterId, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.SelfId.ShouldBeOfType<PlotFolderIdentification>();
        result.ParentId.ShouldBeOfType<ProjectIdentification>();
        if (moveAfterId is not null)
        {
            result.MoveAfterId.ShouldBeOfType<PlotFolderIdentification>();
        }
        else
        {
            result.MoveAfterId.ShouldBeNull();
        }
    }

    /// <summary>
    /// Перестановка вводных внутри своего сюжета (#238): parent — сюжет.
    /// </summary>
    [Theory]
    [InlineData("PlotElementId(1-2-3)", "PlotFolderId(1-2)", null)]
    [InlineData("PlotElementId(1-2-3)", "PlotFolderId(1-2)", "PlotElementId(1-2-4)")]
    public void TryParse_ValidPlotElementInFolderMove_ReturnsTrue(string selfId, string parentId, string? moveAfterId)
    {
        MoveRequest.TryParse(selfId, parentId, moveAfterId, out var result).ShouldBeTrue();
        result.ShouldNotBeNull();
        result.SelfId.ShouldBeOfType<PlotElementIdentification>();
        result.ParentId.ShouldBeOfType<PlotFolderIdentification>();
        if (moveAfterId is not null)
        {
            result.MoveAfterId.ShouldBeOfType<PlotElementIdentification>();
        }
        else
        {
            result.MoveAfterId.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("bad-id", "Project(1)", null)]
    [InlineData("ProjectFieldId(1-2)", "bad-id", null)]
    [InlineData("ProjectFieldId(1-2)", "Project(1)", "bad-id")]
    public void TryParse_UnparsableIds_ReturnsFalse(string selfId, string parentId, string? moveAfterId)
    {
        MoveRequest.TryParse(selfId, parentId, moveAfterId, out var result).ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryParse_MoveAfterIdTypeMismatch_ReturnsFalse()
    {
        // moveAfterId is a Project, but selfId is a field — type mismatch
        var success = MoveRequest.TryParse(
            "ProjectFieldId(1-2)", "Project(1)", "Project(1)",
            out var result);

        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("42")]
    [InlineData("")]
    public void TryParse_BareIntegerOrEmpty_ReturnsFalse(string id)
    {
        MoveRequest.TryParse(id, "Project(1)", null, out var result).ShouldBeFalse();
        result.ShouldBeNull();
    }
}
