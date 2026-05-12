using JoinRpg.DataModel.Mocks;
using JoinRpg.Dal.Impl.Repositories;
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

    [Fact]
    public void Build_ShouldSetAllChildGroupsRecursively()
    {
        // Arrange
        var project = _mock.Project;
        var rootGroup = project.RootGroup;
        var parent1 = _mock.CreateCharacterGroup();
        var parent2 = _mock.CreateCharacterGroup();
        var child = _mock.CreateCharacterGroup();
        var grandchild = _mock.CreateCharacterGroup();
        
        parent1.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];
        parent2.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];
        child.ParentCharacterGroupIds = [parent1.CharacterGroupId, parent2.CharacterGroupId];
        grandchild.ParentCharacterGroupIds = [child.CharacterGroupId];
        
        // Act
        var result = CharacterGroupDictionaryBuilder.Build(project, new ProjectIdentification(project.ProjectId));
        
        // Assert
        var rootGroupId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), rootGroup.CharacterGroupId);
        var parent1Id = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), parent1.CharacterGroupId);
        var parent2Id = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), parent2.CharacterGroupId);
        var childId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), child.CharacterGroupId);
        var grandchildId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), grandchild.CharacterGroupId);
        
        var rootInfo = result[rootGroupId];
        var parent1Info = result[parent1Id];
        var parent2Info = result[parent2Id];
        var childInfo = result[childId];
        var grandchildInfo = result[grandchildId];
        
        // Root should have all descendants
        rootInfo.AllChildGroups.ShouldContain(parent1Id);
        rootInfo.AllChildGroups.ShouldContain(parent2Id);
        rootInfo.AllChildGroups.ShouldContain(childId);
        rootInfo.AllChildGroups.ShouldContain(grandchildId);
        rootInfo.AllChildGroups.Count.ShouldBe(4);
        
        // Parent1 should have child and grandchild
        parent1Info.AllChildGroups.ShouldContain(childId);
        parent1Info.AllChildGroups.ShouldContain(grandchildId);
        parent1Info.AllChildGroups.Count.ShouldBe(2);
        
        // Parent2 should have child and grandchild
        parent2Info.AllChildGroups.ShouldContain(childId);
        parent2Info.AllChildGroups.ShouldContain(grandchildId);
        parent2Info.AllChildGroups.Count.ShouldBe(2);
        
        // Child should have grandchild only
        childInfo.AllChildGroups.ShouldContain(grandchildId);
        childInfo.AllChildGroups.Count.ShouldBe(1);
        
        // Grandchild should have no children
        grandchildInfo.AllChildGroups.ShouldBeEmpty();
        
        // Groups should not include themselves in AllChildGroups
        rootInfo.AllChildGroups.ShouldNotContain(rootGroupId);
        parent1Info.AllChildGroups.ShouldNotContain(parent1Id);
        parent2Info.AllChildGroups.ShouldNotContain(parent2Id);
        childInfo.AllChildGroups.ShouldNotContain(childId);
        grandchildInfo.AllChildGroups.ShouldNotContain(grandchildId);
    }

    [Fact]
    public void Build_ShouldSetAllParentGroupsRecursively()
    {
        // Arrange
        var project = _mock.Project;
        var rootGroup = project.RootGroup;
        var parent1 = _mock.CreateCharacterGroup();
        var parent2 = _mock.CreateCharacterGroup();
        var child = _mock.CreateCharacterGroup();
        var grandchild = _mock.CreateCharacterGroup();
        
        parent1.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];
        parent2.ParentCharacterGroupIds = [rootGroup.CharacterGroupId];
        child.ParentCharacterGroupIds = [parent1.CharacterGroupId, parent2.CharacterGroupId];
        grandchild.ParentCharacterGroupIds = [child.CharacterGroupId];
        
        // Act
        var result = CharacterGroupDictionaryBuilder.Build(project, new ProjectIdentification(project.ProjectId));
        
        // Assert
        var rootGroupId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), rootGroup.CharacterGroupId);
        var parent1Id = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), parent1.CharacterGroupId);
        var parent2Id = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), parent2.CharacterGroupId);
        var childId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), child.CharacterGroupId);
        var grandchildId = new CharacterGroupIdentification(new ProjectIdentification(project.ProjectId), grandchild.CharacterGroupId);
        
        var rootInfo = result[rootGroupId];
        var parent1Info = result[parent1Id];
        var parent2Info = result[parent2Id];
        var childInfo = result[childId];
        var grandchildInfo = result[grandchildId];
        
        // Grandchild should have all ancestors
        grandchildInfo.AllParentGroups.ShouldContain(childId);
        grandchildInfo.AllParentGroups.ShouldContain(parent1Id);
        grandchildInfo.AllParentGroups.ShouldContain(parent2Id);
        grandchildInfo.AllParentGroups.ShouldContain(rootGroupId);
        grandchildInfo.AllParentGroups.Count.ShouldBe(4);
        
        // Child should have parents and root
        childInfo.AllParentGroups.ShouldContain(parent1Id);
        childInfo.AllParentGroups.ShouldContain(parent2Id);
        childInfo.AllParentGroups.ShouldContain(rootGroupId);
        childInfo.AllParentGroups.Count.ShouldBe(3);
        
        // Parent1 should have root only
        parent1Info.AllParentGroups.ShouldContain(rootGroupId);
        parent1Info.AllParentGroups.Count.ShouldBe(1);
        
        // Parent2 should have root only
        parent2Info.AllParentGroups.ShouldContain(rootGroupId);
        parent2Info.AllParentGroups.Count.ShouldBe(1);
        
        // Root should have no parents
        rootInfo.AllParentGroups.ShouldBeEmpty();
        
        // Groups should not include themselves in AllParentGroups
        grandchildInfo.AllParentGroups.ShouldNotContain(grandchildId);
        childInfo.AllParentGroups.ShouldNotContain(childId);
        parent1Info.AllParentGroups.ShouldNotContain(parent1Id);
        parent2Info.AllParentGroups.ShouldNotContain(parent2Id);
        rootInfo.AllParentGroups.ShouldNotContain(rootGroupId);
    }
}