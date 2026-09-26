using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Impl.Projects.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test;

/// <summary>
/// Разовая починка данных под #4878: публичной группе без публичного пути наверх снимается
/// публичность. Состояние «до» собирается прямо на моке — через сервисы его уже не создать,
/// потому что редактирование и создание группы такое запрещают.
/// </summary>
public class PublicGroupVisibilityFixerTest
{
    private readonly MockedProject mock = new();
    private readonly FakeUnitOfWork unitOfWork;
    private readonly FakeProjectMetadataRepository metadataRepository;

    public PublicGroupVisibilityFixerTest()
    {
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
    }

    private CharacterGroup RootGroup => mock.Project.CharacterGroups.Single(g => g.IsRoot);

    private CharacterGroup CreateGroup(bool isPublic, params CharacterGroup[] parents)
    {
        var group = mock.CreateCharacterGroup();
        group.IsPublic = isPublic;
        group.ParentCharacterGroupIds = [.. parents.Select(p => p.CharacterGroupId)];
        mock.ReInitProjectInfo();
        return group;
    }

#pragma warning disable CS0618 // Тест разовой починки, уедет вместе с ней (#4974)
    private PublicGroupVisibilityFixer CreateFixer()
        => new(new ProjectPropsService(
            unitOfWork,
            new FakeCurrentUserAccessor(mock.Master.UserId),
            metadataRepository,
            NullLogger<ProjectPropsService>.Instance));
#pragma warning restore CS0618

    private async Task<IReadOnlyCollection<string>> Fix()
        => await CreateFixer().HideGroupsWithoutPublicPath(mock.ProjectInfo.ProjectId);

    [Fact]
    public async Task HidesPublicGroupUnderPrivateParent()
    {
        var privateParent = CreateGroup(isPublic: false, RootGroup);
        var violating = CreateGroup(isPublic: true, privateParent);

        var hidden = await Fix();

        hidden.ShouldBe([violating.CharacterGroupName]);
        violating.IsPublic.ShouldBeFalse();
        privateParent.IsPublic.ShouldBeFalse();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Чинится вся ветка: у внука путь наверх тоже непубличный.
    /// </summary>
    [Fact]
    public async Task HidesWholeBranch()
    {
        var privateParent = CreateGroup(isPublic: false, RootGroup);
        var child = CreateGroup(isPublic: true, privateParent);
        var grandChild = CreateGroup(isPublic: true, child);

        var hidden = await Fix();

        hidden.ShouldBe([child.CharacterGroupName, grandChild.CharacterGroupName], ignoreOrder: true);
        child.IsPublic.ShouldBeFalse();
        grandChild.IsPublic.ShouldBeFalse();
    }

    /// <summary>
    /// Второй публичный путь наверх — не нарушение, группу не трогаем.
    /// </summary>
    [Fact]
    public async Task KeepsGroupWithSecondPublicPath()
    {
        var privateParent = CreateGroup(isPublic: false, RootGroup);
        var publicParent = CreateGroup(isPublic: true, RootGroup);
        var group = CreateGroup(isPublic: true, privateParent, publicParent);

        var hidden = await Fix();

        hidden.ShouldBeEmpty();
        group.IsPublic.ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task KeepsCleanProjectIntact()
    {
        var group = CreateGroup(isPublic: true, RootGroup);
        var privateGroup = CreateGroup(isPublic: false, group);

        var hidden = await Fix();

        hidden.ShouldBeEmpty();
        group.IsPublic.ShouldBeTrue();
        privateGroup.IsPublic.ShouldBeFalse();
    }

    /// <summary>
    /// Архивные проекты чинить надо — там 215 из 244 нарушений на проде, — поэтому операция
    /// разрешена и над неактивным проектом.
    /// </summary>
    [Fact]
    public async Task WorksOnArchivedProject()
    {
        var privateParent = CreateGroup(isPublic: false, RootGroup);
        var violating = CreateGroup(isPublic: true, privateParent);
        // Архив — это Active = false вместе с закрытым приёмом заявок: другой комбинации
        // ProjectLoaderCommon.CreateStatus не допускает.
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        var hidden = await Fix();

        hidden.ShouldBe([violating.CharacterGroupName]);
        violating.IsPublic.ShouldBeFalse();
    }

    /// <summary>
    /// Спецгруппы правило не считает нарушителями (их публичность задаётся вариантом поля),
    /// значит и починка их не трогает.
    /// </summary>
    [Fact]
    public async Task KeepsSpecialGroup()
    {
        var privateParent = CreateGroup(isPublic: false, RootGroup);
        var special = CreateGroup(isPublic: true, privateParent);
        special.IsSpecial = true;
        mock.ReInitProjectInfo();

        var hidden = await Fix();

        hidden.ShouldBeEmpty();
        special.IsPublic.ShouldBeTrue();
    }
}
