namespace JoinRpg.Web.Models.Dialogs
{
    public class DeleteDialogViewModel : IProjectIdAware
    {
        public int ProjectId { get; set; }
        public string Title { get; set; }
    }
}
