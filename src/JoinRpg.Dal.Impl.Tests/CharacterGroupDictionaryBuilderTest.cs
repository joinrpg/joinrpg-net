using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Tests;

public class CharacterGroupDictionaryBuilderTest
{
    private readonly MockedProject _mock = new MockedProject();
    private readonly ProjectIdentification _projectId = new(1);

    [Fact]
    public void Build_ShouldReturnAllGroups()
    {
        // Arrange
        var project = _mock.Project;
        
        // Act
        var result = CharacterGroupDictionaryBuilder.Build(project, new ProjectIdentification(project.ProjectId));
        
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(project.CharacterGroups.Count);
    }

    [Fact]
    public void Build_ShouldIncludeRootGroup()
    {
        // Arrange
        var project = _mock.Project;
        var rootGroup = project.RootGroup;
        
        // Act
        var result = CharacterGroupDictionaryBuilder.Build(project, new ProjectIdentification(project.ProjectId));
        
        // Assert
        var rootGroupId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), rootGroup.CharacterGroupId);
        result.ShouldContainKey(rootGroupId);
        var rootInfo = result[rootGroupId];
        rootInfo.IsRoot.ShouldBeTrue();
        rootInfo.IsActive.ShouldBeTrue();
        rootInfo.Name.ShouldBe(rootGroup.CharacterGroupName);
    }

    [Fact]
    public void Build_ShouldSetParentChildRelationships()
    {
        // Arrange
        var project = _mock.Project;
        var rootGroup = project.RootGroup;
        var childGroup = _mock.Group;
        childGroup.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];
        
        // Act
        var result = CharacterGroupDictionaryBuilder.Build(project, new ProjectIdentification(project.ProjectId));
        
        // Assert
        var rootGroupId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), rootGroup.CharacterGroupId);
        var childGroupId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), childGroup.CharacterGroupId);
        
        var rootInfo = result[rootGroupId];
        var childInfo = result[childGroupId];
        
        rootInfo.ChildGroupIds.ShouldContain(childGroupId);
        childInfo.ParentGroupIds.ShouldContain(rootGroupId);
    }

    [Fact]
    public void Build_ShouldHandleMultipleParents()
    {
        // Arrange
        var project = _mock.Project;
        var rootGroup = project.RootGroup;
        var parent1 = _mock.CreateCharacterGroup();
        var parent2 = _mock.CreateCharacterGroup();
        var childGroup = _mock.CreateCharacterGroup();
        
        parent1.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];
        parent2.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];
        childGroup.ParentCharacterGroupIds = [parent1.CharacterGroupId, parent2.CharacterGroupId];
        
        // Act
        var result = CharacterGroupDictionaryBuilder.Build(project, new ProjectIdentification(project.ProjectId));
        
        // Assert
        var childGroupId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), childGroup.CharacterGroupId);
        var parent1Id = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), parent1.CharacterGroupId);
        var parent2Id = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), parent2.CharacterGroupId);
        
        var childInfo = result[childGroupId];
        childInfo.ParentGroupIds.ShouldContain(parent1Id);
        childInfo.ParentGroupIds.ShouldContain(parent2Id);
        
        var parent1Info = result[parent1Id];
        parent1Info.ChildGroupIds.ShouldContain(childGroupId);
        
        var parent2Info = result[parent2Id];
        parent2Info.ChildGroupIds.ShouldContain(childGroupId);
    }

    [Fact]
    public void Build_ShouldSetGroupProperties()
    {
        // Arrange
        var project = _mock.Project;
        var group = _mock.CreateCharacterGroup();
        group.IsPublic = true;
        group.IsActive = false;
        group.IsSpecial = true;
        
        // Act
        var result = CharacterGroupDictionaryBuilder.Build(project, new ProjectIdentification(project.ProjectId));
        
        // Assert
        var groupId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), group.CharacterGroupId);
        var groupInfo = result[groupId];
        
        groupInfo.IsPublic.ShouldBeTrue();
        groupInfo.IsActive.ShouldBeFalse();
        groupInfo.IsSpecial.ShouldBeTrue();
        groupInfo.Name.ShouldBe(group.CharacterGroupName);
    }

    [Fact]
    public void Build_ShouldHandleEmptyChildGroups()
    {
        // Arrange
        var project = _mock.Project;
        var leafGroup = _mock.CreateCharacterGroup();
        leafGroup.ParentCharacterGroupIds = [project.RootGroup.CharacterGroupId];
        
        // Act
        var result = CharacterGroupDictionaryBuilder.Build(project, new ProjectIdentification(project.ProjectId));
        
        // Assert
        var leafGroupId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), leafGroup.CharacterGroupId);
        var leafInfo = result[leafGroupId];
        
        leafInfo.ChildGroupIds.ShouldBeEmpty();
    }
}