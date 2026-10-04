using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Роль создателя проекта на странице мастеров (ADR019, §4).
/// </summary>
public class CreateProjectOwnerRoleTest
{
    [Theory]
    [InlineData(ProjectTypeDto.Larp, "Главный мастер")]
    [InlineData(ProjectTypeDto.EmptyProject, "Главный мастер")]
    [InlineData(ProjectTypeDto.CopyFromAnother, "Главный мастер")]
    [InlineData(ProjectTypeDto.Convention, "Главный организатор")]
    [InlineData(ProjectTypeDto.ConventionProgram, "Главный организатор")]
    public void OwnerRoleDependsOnProjectType(ProjectTypeDto projectType, string expectedRole)
        => CreateProjectService.GetOwnerRole(projectType).ShouldBe(expectedRole);
}
