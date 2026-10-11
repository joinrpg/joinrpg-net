using JoinRpg.Web.ProjectMasterTools.Fields;

namespace JoinRpg.WebPortal.Models.FieldSetup;

public class FieldSettingsViewModel : IFieldNavigationAware
{
    public FieldNavigationModel Navigation { get; private set; }

    public int ProjectId { get; set; }

    public void SetNavigation(FieldNavigationModel fieldNavigation)
    {
        Navigation = fieldNavigation;
        Navigation.Page = FieldNavigationPage.FieldSettings;
    }
}
