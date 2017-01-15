using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
  public interface IForumService
  {
    Task<int> CreateThread(int projectId, int characterGroupId, string header, string commentText, bool hideFromUser);
    Task AddComment(int projectId, int forumThreadId, int? parentCommentId, bool isVisibleToPlayer, string commentText);
  }
}
