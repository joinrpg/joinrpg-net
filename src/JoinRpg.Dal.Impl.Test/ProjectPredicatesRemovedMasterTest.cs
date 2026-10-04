using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.ProjectMetadata;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Снятый мастер (ADR019) не попадает ни в один список «мои проекты как мастера», даже если у строки остались права.
/// </summary>
public class ProjectPredicatesRemovedMasterTest
{
    private static readonly UserIdentification Master = new(1);

    public static TheoryData<string> MasterSpecifications =>
    [
        nameof(ProjectListSpecification.AllProjectsWithMasterAccess),
        nameof(ProjectListSpecification.ActiveWithMyMasterAccess),
        nameof(ProjectListSpecification.ActiveProjectsWithGrantMasterAccess),
        nameof(ProjectListSpecification.ActiveProjectsWithManageClaimsAccess),
        nameof(ProjectListSpecification.MyAllProjects),
        nameof(ProjectListSpecification.ForCloning),
    ];

    [Theory]
    [MemberData(nameof(MasterSpecifications))]
    public void ActiveMaster_IsSelected(string specification)
        => Matches(specification, CreateProject(ProjectAclStatus.Active)).ShouldBeTrue();

    [Theory]
    [MemberData(nameof(MasterSpecifications))]
    public void RemovedMaster_IsNotSelected(string specification)
        => Matches(specification, CreateProject(ProjectAclStatus.Removed)).ShouldBeFalse();

    private static bool Matches(string specification, Project project)
    {
        ProjectListSpecification spec = specification switch
        {
            nameof(ProjectListSpecification.AllProjectsWithMasterAccess) => ProjectListSpecification.AllProjectsWithMasterAccess(Master),
            nameof(ProjectListSpecification.ActiveWithMyMasterAccess) => ProjectListSpecification.ActiveWithMyMasterAccess(Master),
            nameof(ProjectListSpecification.ActiveProjectsWithGrantMasterAccess) => ProjectListSpecification.ActiveProjectsWithGrantMasterAccess(Master),
            nameof(ProjectListSpecification.ActiveProjectsWithManageClaimsAccess) => ProjectListSpecification.ActiveProjectsWithManageClaimsAccess(Master),
            nameof(ProjectListSpecification.MyAllProjects) => ProjectListSpecification.MyAllProjects(Master),
            nameof(ProjectListSpecification.ForCloning) => ProjectListSpecification.ForCloning(Master),
            _ => throw new ArgumentOutOfRangeException(nameof(specification)),
        };
        return ProjectPredicates.BySpecification(spec).Compile()(project);
    }

    // Права у снятого мастера оставлены нарочно: фильтр обязан смотреть на статус, а не только на права.
    private static Project CreateProject(ProjectAclStatus status) => new()
    {
        Active = true,
        // Клонировать может только мастер — иначе ForCloning не смотрел бы на ACL.
        Details = new DataModel.ProjectDetails() { ProjectCloneSettings = ProjectCloneSettings.CanBeClonedByMaster },
        Claims = [],
        ProjectAcls = [new ProjectAcl
        {
            UserId = Master.Value,
            Role = "Мастер",
            Status = status,
            CanGrantRights = true,
            CanManageClaims = true,
        }],
    };
}
