namespace JoinRpg.Web.Models.FieldSetup
{
    public interface IFieldNavigationAware : IProjectIdAware
    {
        FieldNavigationModel Navigation { get; }
        new int ProjectId { get; set; }

        void SetNavigation(FieldNavigationModel fieldNavigation);
    }
}
