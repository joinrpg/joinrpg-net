using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

// Баг #5039: в чужом профиле карточка проекта показывала «Мой проект», хотя мастерские права
// на этой игре есть у хозяина профиля, а не у того, кто смотрит. Персонализация считалась
// по тому же пользователю, по которому шел отбор проектов.
public class PersonalizedProjectListSpecificationTest
{
    private static readonly UserIdentification ProfileOwner = new(1);
    private static readonly UserIdentification Viewer = new(2);

    private static Project CreateProjectWithMaster(UserIdentification master) => new()
    {
        Active = true,
        Details = new ProjectDetails(),
        ProjectAcls = [new ProjectAcl { UserId = master.Value }],
    };

    private static bool Matches(ProjectListSpecification specification, Project project)
        => ProjectPredicates.BySpecification(specification).Compile()(project);

    [Fact]
    public void ByDefaultPersonalizedForSameUser()
        => ProjectListSpecification.AllProjectsWithMasterAccess(ProfileOwner)
            .PersonalizeForUser.ShouldBe(ProfileOwner);

    [Fact]
    public void PersonalizedForChangesOnlyPersonalization()
    {
        var specification = ProjectListSpecification.AllProjectsWithMasterAccess(ProfileOwner).PersonalizedFor(Viewer);

        specification.UserId.ShouldBe(ProfileOwner);
        specification.PersonalizeForUser.ShouldBe(Viewer);
    }

    [Fact]
    public void PersonalizedForAnonymousIsAllowed()
        => ProjectListSpecification.AllProjectsWithMasterAccess(ProfileOwner)
            .PersonalizedFor(null).PersonalizeForUser.ShouldBeNull();

    [Fact]
    public void FilterStillSelectsProjectsOfProfileOwner()
        => Matches(
            ProjectListSpecification.AllProjectsWithMasterAccess(ProfileOwner).PersonalizedFor(Viewer),
            CreateProjectWithMaster(ProfileOwner)).ShouldBeTrue();

    [Fact]
    public void FilterDoesNotSelectProjectsOfViewer()
        => Matches(
            ProjectListSpecification.AllProjectsWithMasterAccess(ProfileOwner).PersonalizedFor(Viewer),
            CreateProjectWithMaster(Viewer)).ShouldBeFalse();
}
