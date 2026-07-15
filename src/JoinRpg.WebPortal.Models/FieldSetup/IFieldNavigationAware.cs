using JoinRpg.Web.Models;
using JoinRpg.Web.ProjectMasterTools.Fields;

namespace JoinRpg.WebPortal.Models.FieldSetup;

public interface IFieldNavigationAware : IProjectIdAware
{
    FieldNavigationModel Navigation { get; }
    new int ProjectId { get; set; }

    void SetNavigation(FieldNavigationModel fieldNavigation);
}
