using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Controllers.Comments;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Route("{ProjectId}/goto/")]
    public class DiscussionRedirectController : ControllerGameBase
    {
        public DiscussionRedirectController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IUserRepository userRepository,
            IForumRepository forumRepository)
            : base(projectRepository, projectService, userRepository)
        {
            this.forumRepository = forumRepository;
        }

        private readonly IForumRepository forumRepository;

        [HttpGet("discussion/{CommentDiscussionId}")]
        public async Task<ActionResult> ToDiscussion(int projectId, int commentDiscussionId)
        {
            CommentDiscussion discussion = await forumRepository.GetDiscussion(projectId, commentDiscussionId);

            if (!discussion.HasAnyAccess(CurrentUserId))
            {
                return NoAccesToProjectView(discussion.Project);
            }

            return CommentRedirectHelper.RedirectToDiscussion(Url, discussion, commentId: null);
        }

        [HttpGet("comment/{CommentId}")]
        [Authorize]
        public async Task<ActionResult> ToComment(int projectid, int commentid)
        {
            CommentDiscussion discussion =  await forumRepository.GetDiscussionByComment(projectid, commentid);

            if (!discussion.HasAnyAccess(CurrentUserId))
            {
                return NoAccesToProjectView(discussion.Project);
            }

            return CommentRedirectHelper.RedirectToDiscussion(Url, discussion, commentid);
        }
    }
}
