using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebPortal.Managers.Projects;

namespace JoinRpg.WebPortal.Managers.Test;

public class ProjectListViewServiceTests
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<ProjectSelectionCriteria>))]
    public void HasSpecification(ProjectSelectionCriteria criteria)
    {
        ProjectListViewService.GetSpecification(new PrimitiveTypes.UserIdentification(1), criteria).ShouldNotBeNull();
    }
}
