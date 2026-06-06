using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using System.Text.Json;

namespace JoinRpg.DomainTypes.Test;

public class FieldLayerContainerTest
{
    [Fact]
    public void ShouldCreateLayerDataWithSingleField()
    {
        var projectInfo = MakeProject(MakeField(1));
        var layerData = new Dictionary<int, string> { { 1, "value1" } };

        var container = new FieldLayerContainer(projectInfo, layerData);

        container.LayerData.ShouldHaveSingleItem();
        container.LayerData[new ProjectFieldIdentification(1, 1)].Value.ShouldBe("value1");
    }

    [Fact]
    public void ShouldCreateLayerDataWithMultipleFields()
    {
        var projectInfo = MakeProject(MakeField(1), MakeField(2));
        var layerData = new Dictionary<int, string>
        {
            { 1, "alpha" },
            { 2, "beta" },
        };

        var container = new FieldLayerContainer(projectInfo, layerData);

        container.LayerData.Count.ShouldBe(2);
        container.LayerData[new ProjectFieldIdentification(1, 1)].Value.ShouldBe("alpha");
        container.LayerData[new ProjectFieldIdentification(1, 2)].Value.ShouldBe("beta");
    }

    [Fact]
    public void ShouldSkipMissingValues()
    {
        var projectInfo = MakeProject(MakeField(1), MakeField(2));
        var layerData = new Dictionary<int, string> { { 1, "only me" } };

        var container = new FieldLayerContainer(projectInfo, layerData);

        container.LayerData.Count.ShouldBe(1);
        container.LayerData.ShouldContainKey(new ProjectFieldIdentification(1, 1));
        container.LayerData.ShouldNotContainKey(new ProjectFieldIdentification(1, 2));
    }

    [Fact]
    public void ShouldThrowForUnknownFieldId()
    {
        var projectInfo = MakeProject(MakeField(1));
        var layerData = new Dictionary<int, string> { { 999, "ghost" } };

        var ex = Should.Throw<KeyNotFoundException>(() =>
            _ = new FieldLayerContainer(projectInfo, layerData));

        ex.Message.ShouldContain("999");
    }

    [Fact]
    public void ShouldHandleCheckboxValue()
    {
        var projectInfo = MakeProject(MakeField(1, ProjectFieldType.Checkbox));
        var layerData = new Dictionary<int, string> { { 1, "on" } };

        var container = new FieldLayerContainer(projectInfo, layerData);

        var fwv = container.LayerData[new ProjectFieldIdentification(1, 1)];
        fwv.Value.ShouldBe("on");
        fwv.DisplayString.ShouldBe("☑️");
    }

    [Fact]
    public void ShouldPopulateFieldMetadata()
    {
        var projectInfo = MakeProject(MakeField(42, ProjectFieldType.String));
        var layerData = new Dictionary<int, string> { { 42, "hello" } };

        var container = new FieldLayerContainer(projectInfo, layerData);

        var fwv = container.LayerData[new ProjectFieldIdentification(1, 42)];
        fwv.Field.Name.ShouldBe("Field42");
        fwv.Field.Type.ShouldBe(ProjectFieldType.String);
    }

    [Fact]
    public void PublicOnlyShouldExcludeNonPublicFields()
    {
        var projectInfo = MakeProject(
            MakeField(1, visibility: ProjectFieldVisibility.Public),
            MakeField(2, visibility: ProjectFieldVisibility.MasterOnly));
        var layerData = new Dictionary<int, string> { { 1, "public" }, { 2, "secret" } };

        var result = new FieldLayerContainer(projectInfo, layerData).PublicOnly();

        result.LayerData.Count.ShouldBe(1);
        result.LayerData.ShouldContainKey(new ProjectFieldIdentification(1, 1));
        result.LayerData.ShouldNotContainKey(new ProjectFieldIdentification(1, 2));
    }

    [Fact]
    public void PublicOnlyShouldExcludePlayerAndMasterFields()
    {
        var projectInfo = MakeProject(
            MakeField(1, visibility: ProjectFieldVisibility.Public),
            MakeField(2, visibility: ProjectFieldVisibility.PlayerAndMaster));
        var layerData = new Dictionary<int, string> { { 1, "pub" }, { 2, "restricted" } };

        var result = new FieldLayerContainer(projectInfo, layerData).PublicOnly();

        result.LayerData.Count.ShouldBe(1);
        result.LayerData.ShouldContainKey(new ProjectFieldIdentification(1, 1));
    }

    [Fact]
    public void PublicOnlyShouldReturnEmptyWhenNoPublicFields()
    {
        var projectInfo = MakeProject(
            MakeField(1, visibility: ProjectFieldVisibility.MasterOnly));
        var layerData = new Dictionary<int, string> { { 1, "secret" } };

        var result = new FieldLayerContainer(projectInfo, layerData).PublicOnly();

        result.LayerData.ShouldBeEmpty();
    }

    [Fact]
    public void PublicOnlyShouldPreserveValues()
    {
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.Public));
        var layerData = new Dictionary<int, string> { { 1, "hello" } };

        var result = new FieldLayerContainer(projectInfo, layerData).PublicOnly();

        result.LayerData[new ProjectFieldIdentification(1, 1)].Value.ShouldBe("hello");
    }

[Fact]
    public void ShouldReturnNewInstanceWhenFiltering()
    {
        var projectInfo = MakeProject(
            MakeField(1, visibility: ProjectFieldVisibility.Public),
            MakeField(2, visibility: ProjectFieldVisibility.MasterOnly));
        var original = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "x" }, { 2, "y" } });

        var result = original.PublicOnly();

        result.ShouldNotBeSameAs(original);
    }

[Fact]
    public void ShouldKeepSameProjectInfo()
    {
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.Public));
        var original = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "x" } });

        var result = original.PublicOnly();

        result.ProjectInfo.ShouldBeSameAs(projectInfo);
    }

    [Fact]
    public void ShouldDeserializeValidJson()
    {
        var projectInfo = MakeProject(MakeField(1), MakeField(2));
        var json = """{"1":"alpha","2":"beta"}""";

        var container = FieldLayerContainer.DeserializeFieldLayer(projectInfo, json);

        container.LayerData.Count.ShouldBe(2);
        container.LayerData[new ProjectFieldIdentification(1, 1)].Value.ShouldBe("alpha");
        container.LayerData[new ProjectFieldIdentification(1, 2)].Value.ShouldBe("beta");
    }

    [Fact]
    public void ShouldDeserializeNullToEmpty()
    {
        var projectInfo = MakeProject(MakeField(1));

        var container = FieldLayerContainer.DeserializeFieldLayer(projectInfo, null!);

        container.LayerData.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldDeserializeEmptyStringToEmpty()
    {
        var projectInfo = MakeProject(MakeField(1));

        var container = FieldLayerContainer.DeserializeFieldLayer(projectInfo, "");

        container.LayerData.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldThrowForUnknownFieldIdInDeserializedJson()
    {
        var projectInfo = MakeProject(MakeField(1));
        var json = """{"999":"ghost"}""";

        var ex = Should.Throw<KeyNotFoundException>(() =>
            FieldLayerContainer.DeserializeFieldLayer(projectInfo, json));

        ex.Message.ShouldContain("999");
    }

    [Fact]
    public void ShouldDeserializeWithCheckboxValue()
    {
        var projectInfo = MakeProject(MakeField(1, ProjectFieldType.Checkbox));
        var json = """{"1":"on"}""";

        var container = FieldLayerContainer.DeserializeFieldLayer(projectInfo, json);

        var fwv = container.LayerData[new ProjectFieldIdentification(1, 1)];
        fwv.Value.ShouldBe("on");
        fwv.DisplayString.ShouldBe("☑️");
    }

    [Fact]
    public void ShouldDeserializeAndPreserveProjectInfo()
    {
        var projectInfo = MakeProject(MakeField(1));
        var json = """{"1":"val"}""";

        var container = FieldLayerContainer.DeserializeFieldLayer(projectInfo, json);

        container.ProjectInfo.ShouldBeSameAs(projectInfo);
    }

    [Fact]
    public void ShouldThrowJsonExceptionForWhitespaceString()
    {
        var projectInfo = MakeProject(MakeField(1));

        Should.Throw<JsonException>(() =>
            FieldLayerContainer.DeserializeFieldLayer(projectInfo, "   "));
    }

    [Fact]
    public void ShouldDeserializeWithSingleField()
    {
        var projectInfo = MakeProject(MakeField(42, ProjectFieldType.String));
        var json = """{"42":"hello"}""";

        var container = FieldLayerContainer.DeserializeFieldLayer(projectInfo, json);

        container.LayerData.ShouldHaveSingleItem();
        container.LayerData[new ProjectFieldIdentification(1, 42)].Value.ShouldBe("hello");
    }

    [Fact]
    public void ShouldDeserializeAndPopulateFieldMetadata()
    {
        var projectInfo = MakeProject(MakeField(42, ProjectFieldType.String));
        var json = """{"42":"hello"}""";

        var container = FieldLayerContainer.DeserializeFieldLayer(projectInfo, json);

        var fwv = container.LayerData[new ProjectFieldIdentification(1, 42)];
        fwv.Field.Name.ShouldBe("Field42");
        fwv.Field.Type.ShouldBe(ProjectFieldType.String);
    }

    private static ProjectInfo MakeProject(params ProjectFieldInfo[] fields)
    {
        var projectId = new ProjectIdentification(1);
        return new ProjectInfo(
            projectId,
            new ProjectName("Test"),
            "",
            fields,
            new ProjectFieldSettings(null, null),
            new ProjectFinanceSettings(false, []),
            false,
            false,
            new CharacterGroupIdentification(projectId, 1),
            [],
            false,
            new ProjectCheckInSettings(false, false, false),
            ProjectLifecycleStatus.ActiveClaimsOpen,
            new ProjectScheduleSettings(false),
            ProjectCloneSettings.CloneDisabled,
            new DateOnly(2024, 1, 1),
            ProjectProfileRequirementSettings.AllNotRequired,
            new ProjectClaimSettings(null, false, false, false, false),
            [],
            new Dictionary<CharacterGroupIdentification, CharacterGroupInfo>()
        );
    }

    private static ProjectFieldInfo MakeField(int fieldId, ProjectFieldType type = ProjectFieldType.String, ProjectFieldVisibility visibility = ProjectFieldVisibility.Public)
    {
        var projectId = new ProjectIdentification(1);
        var id = new ProjectFieldIdentification(projectId, fieldId);
        return new ProjectFieldInfo(
            id,
            $"Field{fieldId}",
            type,
            FieldBoundTo.Character,
            [],
            "",
            0,
            true,
            true,
            MandatoryStatus.Optional,
            true,
            true,
            [],
            new MarkdownString(),
            new MarkdownString(),
            false,
            new ProjectFieldSettings(null, null),
            null,
            visibility,
            null
        );
    }
}

