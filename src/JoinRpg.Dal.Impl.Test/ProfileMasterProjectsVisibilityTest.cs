using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.ProjectMetadata;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// «Мастер в проектах» в профиле (ADR019, §3): непубличное мастерство видят только мастера того же проекта.
/// </summary>
public class ProfileMasterProjectsVisibilityTest
{
    private static readonly UserIdentification ProfileOwner = new(1);
    private static readonly UserIdentification CoMaster = new(2);
    private static readonly UserIdentification Stranger = new(3);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Owner_SeesOwnMastership(bool isPublic)
        => Matches(viewer: ProfileOwner, CreateProject(isPublic)).ShouldBeTrue();

    [Fact]
    public void PublicMastership_IsVisibleToAnonymousAndStrangers()
    {
        Matches(viewer: null, CreateProject(isPublic: true)).ShouldBeTrue();
        Matches(viewer: Stranger, CreateProject(isPublic: true)).ShouldBeTrue();
    }

    [Fact]
    public void NonPublicMastership_IsHiddenFromAnonymousAndStrangers()
    {
        Matches(viewer: null, CreateProject(isPublic: false)).ShouldBeFalse();
        Matches(viewer: Stranger, CreateProject(isPublic: false)).ShouldBeFalse();
    }

    [Fact]
    public void NonPublicMastership_IsVisibleToCoMaster()
        => Matches(viewer: CoMaster, CreateProject(isPublic: false)).ShouldBeTrue();

    [Fact]
    public void NonPublicMastership_IsHiddenFromRemovedCoMaster()
        => Matches(viewer: CoMaster, CreateProject(isPublic: false, coMasterStatus: ProjectAclStatus.Removed)).ShouldBeFalse();

    [Fact]
    public void RemovedMastership_IsHiddenEvenWhenPublic()
        => Matches(viewer: CoMaster, CreateProject(isPublic: true, ownerStatus: ProjectAclStatus.Removed)).ShouldBeFalse();

    private static bool Matches(UserIdentification? viewer, Project project)
        => ProjectPredicates.BySpecification(
            ProjectListSpecification.MasterProjectsForProfile(ProfileOwner).PersonalizedFor(viewer))
            .Compile()(project);

    private static Project CreateProject(
        bool isPublic,
        ProjectAclStatus ownerStatus = ProjectAclStatus.Active,
        ProjectAclStatus coMasterStatus = ProjectAclStatus.Active) => new()
        {
            Active = true,
            Details = new DataModel.ProjectDetails(),
            ProjectAcls =
            [
                new ProjectAcl { UserId = ProfileOwner.Value, Role = "Мастер", IsPublic = isPublic, Status = ownerStatus },
                new ProjectAcl { UserId = CoMaster.Value, Role = "Мастер", Status = coMasterStatus },
            ],
        };
}
