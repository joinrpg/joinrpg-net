namespace JoinRpg.Services.Interfaces
{
    public interface IForumService
    {
        Task<int> CreateThread(int projectId, int characterGroupId, string header, string commentText, bool hideFromUser, bool emailEverybody);
        Task AddComment(int projectId, int forumThreadId, int? parentCommentId, bool isVisibleToPlayer, string commentText);
    }
}
