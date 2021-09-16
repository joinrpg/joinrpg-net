namespace JoinRpg.Services.Interfaces
{
    public interface IPublishPlotElementModel : IPlotElementModel
    {

        bool SendNotification { get; }

        string CommentText { get; }
    }
}
