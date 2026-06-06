using System.Text.Json;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

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

        var container = FieldLayerContainer.DeserializeFieldLayer(projectInfo, null);

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

    [Fact]
    public void GetSortedFieldsForViewShouldRespectCustomOrdering()
    {
        var projectInfo = MakeProject(
            "2,1",
            MakeField(1),
            MakeField(2));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "a" }, { 2, "b" } });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result[0].Field.Id.ProjectFieldId.ShouldBe(2);
        result[1].Field.Id.ProjectFieldId.ShouldBe(1);
    }

    [Fact]
    public void GetSortedFieldsForViewShouldReturnAllFieldsWithValues()
    {
        var projectInfo = MakeProject(MakeField(1), MakeField(2));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "a" }, { 2, "b" } });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result.Count.ShouldBe(2);
        result[0].Value.ShouldBe("a");
        result[1].Value.ShouldBe("b");
    }

    [Fact]
    public void GetSortedFieldsForViewShouldExcludeMasterOnlyForPlayer()
    {
        var projectInfo = MakeProject(
            MakeField(1, visibility: ProjectFieldVisibility.Public),
            MakeField(2, visibility: ProjectFieldVisibility.MasterOnly));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "pub" }, { 2, "secret" } });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsPlayer);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldHaveSingleItem();
        result[0].Field.Id.ProjectFieldId.ShouldBe(1);
    }

    [Fact]
    public void GetSortedFieldsForViewShouldIncludeMasterOnlyForMaster()
    {
        var projectInfo = MakeProject(
            MakeField(1, visibility: ProjectFieldVisibility.Public),
            MakeField(2, visibility: ProjectFieldVisibility.MasterOnly));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "pub" }, { 2, "secret" } });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsMaster);

        var result = layers.GetSortedFieldsForView().ToList();

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void GetSortedFieldsForViewShouldIncludePlayerAndMasterFieldsForMaster()
    {
        var projectInfo = MakeProject(
            MakeField(1, visibility: ProjectFieldVisibility.PlayerAndMaster));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "restricted" } });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsMaster);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldHaveSingleItem();
    }

    [Fact]
    public void GetSortedFieldsForViewShouldExcludeEmptyValueFields()
    {
        var projectInfo = MakeProject(MakeField(1));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string>());
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetSortedFieldsForViewShouldIncludeHeaderFields()
    {
        var projectInfo = MakeProject(MakeField(1, ProjectFieldType.Header));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldHaveSingleItem();
    }

    [Fact]
    public void GetSortedFieldsForViewShouldReturnFieldsSortedById()
    {
        var projectInfo = MakeProject(MakeField(2), MakeField(1));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "x" }, { 2, "y" } });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result[0].Field.Id.ProjectFieldId.ShouldBe(1);
        result[1].Field.Id.ProjectFieldId.ShouldBe(2);
    }

    [Fact]
    public void GetSortedFieldsForViewShouldPreferClaimLayerValue()
    {
        var projectInfo = MakeProject(MakeField(1));
        var claimLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "from-claim" } });
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "from-character" } });
        var layers = new CharacterFieldLayers(claimLayer, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldHaveSingleItem();
        result[0].Value.ShouldBe("from-claim");
    }

    [Fact]
    public void GetSortedFieldsForViewShouldReturnEmptyWhenNoViewableFields()
    {
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.MasterOnly));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "secret" } });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsPlayer);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetSortedFieldsForViewShouldWorkWithNullClaimLayer()
    {
        var projectInfo = MakeProject(MakeField(1));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "val" } });
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldHaveSingleItem();
    }

    [Fact]
    public void GetSortedFieldsForViewShouldIncludeFieldBoundToClaimFromClaimLayer()
    {
        var projectInfo = MakeProject(MakeField(1, boundTo: FieldBoundTo.Claim));
        var claimLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "claim-val" } });
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string>());
        var layers = new CharacterFieldLayers(claimLayer, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldHaveSingleItem();
        result[0].Value.ShouldBe("claim-val");
    }

    [Fact]
    public void GetSortedFieldsForViewShouldExcludeFieldBoundToClaimWhenClaimLayerNull()
    {
        var projectInfo = MakeProject(MakeField(1, boundTo: FieldBoundTo.Claim));
        var characterLayer = new FieldLayerContainer(projectInfo, new Dictionary<int, string>());
        var layers = new CharacterFieldLayers(null, characterLayer, AccessArgumentsNone);

        var result = layers.GetSortedFieldsForView().ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetFromLayerShouldReturnValueForPublicFieldWithAnyAccess()
    {
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.Public));
        var container = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "val" } });

        var result = container.GetFromLayer(new ProjectFieldIdentification(1, 1), AccessArgumentsNone);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe("val");
    }

    [Fact]
    public void GetFromLayerShouldReturnNullForMasterOnlyFieldWithPlayerAccess()
    {
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.MasterOnly));
        var container = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "secret" } });

        var result = container.GetFromLayer(new ProjectFieldIdentification(1, 1), AccessArgumentsPlayer);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetFromLayerShouldReturnValueForMasterOnlyFieldWithMasterAccess()
    {
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.MasterOnly));
        var container = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "secret" } });

        var result = container.GetFromLayer(new ProjectFieldIdentification(1, 1), AccessArgumentsMaster);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe("secret");
    }

    [Fact]
    public void GetFromLayerShouldReturnNullForMissingField()
    {
        var projectInfo = MakeProject(MakeField(1));
        var container = new FieldLayerContainer(projectInfo, new Dictionary<int, string>());

        var result = container.GetFromLayer(new ProjectFieldIdentification(1, 1), AccessArgumentsMaster);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetFromLayerShouldReturnNullForPlayerAndMasterFieldWithNoAccess()
    {
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.PlayerAndMaster));
        var container = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "restricted" } });

        var result = container.GetFromLayer(new ProjectFieldIdentification(1, 1), AccessArgumentsNone);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetFromLayerShouldReturnValueForPlayerAndMasterFieldWithMasterAccess()
    {
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.PlayerAndMaster));
        var container = new FieldLayerContainer(projectInfo, new Dictionary<int, string> { { 1, "restricted" } });

        var result = container.GetFromLayer(new ProjectFieldIdentification(1, 1), AccessArgumentsMaster);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetFromLayerShouldReturnHeaderFieldWithNullValueWhenNotInLayer()
    {
        var projectInfo = MakeProject(MakeField(1, ProjectFieldType.Header));
        var container = new FieldLayerContainer(projectInfo, new Dictionary<int, string>());

        var result = container.GetFromLayer(new ProjectFieldIdentification(1, 1), AccessArgumentsNone);

        result.ShouldNotBeNull();
        result!.Value.ShouldBeNull();
        result.Field.Type.ShouldBe(ProjectFieldType.Header);
    }

    [Fact]
    public void GetFromLayerShouldReturnNullForHeaderWithoutAccess()
    {
        var projectInfo = MakeProject(MakeField(1, ProjectFieldType.Header, visibility: ProjectFieldVisibility.MasterOnly));
        var container = new FieldLayerContainer(projectInfo, new Dictionary<int, string>());

        var result = container.GetFromLayer(new ProjectFieldIdentification(1, 1), AccessArgumentsPlayer);

        result.ShouldBeNull();
    }

    private static ProjectInfo MakeProject(params ProjectFieldInfo[] fields)
        => MakeProject("", fields);

    private static ProjectInfo MakeProject(string ordering, params ProjectFieldInfo[] fields)
    {
        var projectId = new ProjectIdentification(1);
        return new ProjectInfo(
            projectId,
            new ProjectName("Test"),
ordering,
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

    private static ProjectFieldInfo MakeField(int fieldId, ProjectFieldType type = ProjectFieldType.String, ProjectFieldVisibility visibility = ProjectFieldVisibility.Public, FieldBoundTo boundTo = FieldBoundTo.Character, string ordering = "")
    {
        var projectId = new ProjectIdentification(1);
        var id = new ProjectFieldIdentification(projectId, fieldId);
        return new ProjectFieldInfo(
            id,
            $"Field{fieldId}",
            type,
            boundTo,
            [],
            ordering,
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

    private static readonly AccessArguments AccessArgumentsNone = AccessArguments.None;

    private static readonly AccessArguments AccessArgumentsPlayer = new(
        MasterAccess: false,
        PlayerAccessToCharacter: true,
        PlayerAccesToClaim: false,
        EditAllowed: false,
        Published: false,
        CharacterPublic: false,
        IsCapitan: false);

    private static readonly AccessArguments AccessArgumentsMaster = new(
        MasterAccess: true,
        PlayerAccessToCharacter: false,
        PlayerAccesToClaim: false,
        EditAllowed: false,
        Published: false,
        CharacterPublic: false,
        IsCapitan: false);
}
