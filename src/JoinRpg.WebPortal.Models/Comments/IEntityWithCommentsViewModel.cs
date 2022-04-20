namespace JoinRpg.Web.Models
{
    public interface IEntityWithCommentsViewModel
    {
        int ProjectId { get; }
        bool HasMasterAccess { get; }
        IReadOnlyCollection<CommentViewModel> RootComments { get; }
        int CommentDiscussionId { get; }
    }
}

