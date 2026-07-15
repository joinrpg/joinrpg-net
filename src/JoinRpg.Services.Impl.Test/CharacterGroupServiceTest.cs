using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Impl.Projects.Metadata;
using JoinRpg.Services.Impl.Test.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test;

public class CharacterGroupServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeUnitOfWork unitOfWork;
    private readonly FakeProjectMetadataRepository metadataRepository;

    public CharacterGroupServiceTest()
    {
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
    }

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private CharacterGroupIdentification GroupId(CharacterGroup group)
        => new(ProjectId, group.CharacterGroupId);

    private CharacterIdentification CharacterId(Character character)
        => new(ProjectId, character.CharacterId);

    private CharacterGroupIdentification RootGroupId
        => GroupId(mock.Project.CharacterGroups.Single(g => g.IsRoot));

    private CharacterGroupService CreateService(int currentUserId, bool isAdmin = false)
    {
        var propsService = new ProjectPropsService(
            unitOfWork,
            new FakeCurrentUserAccessor(currentUserId, isAdmin),
            metadataRepository,
            NullLogger<ProjectPropsService>.Instance);
        return new CharacterGroupService(propsService);
    }

    [Fact]
    public async Task AddCharacterGroup_AsMaster_CreatesGroupAndPrimesCache()
    {
        var service = CreateService(mock.Master.UserId);

        _ = await service.AddCharacterGroup(
            ProjectId,
            "Новая группа",
            isPublic: true,
            [RootGroupId],
            "Описание");

        var created = mock.Project.CharacterGroups.SingleOrDefault(g => g.CharacterGroupName == "Новая группа");
        created.ShouldNotBeNull();
        created!.IsPublic.ShouldBeTrue();
        created.IsActive.ShouldBeTrue();
        created.IsRoot.ShouldBeFalse();
        created.IsSpecial.ShouldBeFalse();

        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        metadataRepository.LastPrimed.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddCharacterGroup_WithoutMasterAccess_Throws_AndDoesNotSave()
    {
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.AddCharacterGroup(
            ProjectId,
            "Новая группа",
            isPublic: true,
            [RootGroupId],
            "Описание"));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task EditCharacterGroup_SettingOwnChildAsParent_ThrowsCycleViolation()
    {
        var parent = mock.Group;
        var child = mock.CreateCharacterGroup();
        child.ParentCharacterGroupIds = [parent.CharacterGroupId];
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        _ = await Should.ThrowAsync<ArgumentException>(() => service.EditCharacterGroup(
            GroupId(parent),
            "Переименованная",
            isPublic: false,
            [GroupId(child)],
            "Описание"));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task EditCharacterGroup_ValidParent_UpdatesFieldsAndSaves()
    {
        var group = mock.Group;
        group.ParentCharacterGroupIds = [RootGroupId.CharacterGroupId];
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        await service.EditCharacterGroup(
            GroupId(group),
            "Переименованная",
            isPublic: true,
            [RootGroupId],
            "Новое описание");

        group.CharacterGroupName.ShouldBe("Переименованная");
        group.IsPublic.ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task EditCharacterGroup_RootGroup_Throws_AndDoesNotSave()
    {
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<InvalidOperationException>(() => service.EditCharacterGroup(
            RootGroupId,
            "Нельзя",
            isPublic: false,
            [RootGroupId],
            "Описание"));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task MoveCharacterGroup_ReordersChildren()
    {
        var parent = mock.Group;
        var first = mock.CreateCharacterGroup();
        var second = mock.CreateCharacterGroup();
        first.ParentCharacterGroupIds = [parent.CharacterGroupId];
        second.ParentCharacterGroupIds = [parent.CharacterGroupId];
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        await service.MoveCharacterGroup(GroupId(second), GroupId(parent), direction: -1);

        parent.ChildGroupsOrdering.ShouldNotBeNullOrEmpty();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task MoveCharacterAfter_ReordersCharacters()
    {
        var parent = mock.Group;
        var first = mock.CreateCharacter("Первый");
        var second = mock.CreateCharacter("Второй");
        first.ParentCharacterGroupIds = [parent.CharacterGroupId];
        second.ParentCharacterGroupIds = [parent.CharacterGroupId];
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        var newOrder = await service.MoveCharacterAfter(GroupId(parent), CharacterId(first), CharacterId(second));

        newOrder.ShouldBe([CharacterId(second), CharacterId(first)]);
        parent.ChildCharactersOrdering.ShouldNotBeNullOrEmpty();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }
}
