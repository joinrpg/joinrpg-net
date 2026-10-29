using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Comments.Test;

internal static class CommentTestData
{
    public static readonly ProjectIdentification ProjectId = new(123);
    public static readonly CommentDiscussionId DiscussionId = new(45);

    public static DiscussionViewModel Discussion(bool hasMasterAccess, params CommentViewModel[] rootComments)
        => new(ProjectId, DiscussionId, hasMasterAccess, rootComments);

    public static CommentViewModel Comment(
        int id,
        bool isVisibleToPlayer = true,
        bool isRead = true,
        int depth = 0,
        bool canConceal = false,
        params CommentViewModel[] replies)
        => new(
            new CommentId(id),
            new UserLinkViewModel(new UserIdentification(7), "Автор", ViewMode.Show),
            new Uri("https://avatars.example.com/7.png"),
            new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
            IsCommentByPlayer: false,
            isVisibleToPlayer,
            isRead,
            ExtraActionText: null,
            TextHtml: $"<p>Текст {id}</p>",
            Finance: null,
            canConceal,
            depth,
            replies);

    public static void AddCommentServices(this BunitContext context)
    {
        _ = context.Services
            .AddSingleton<ICommentUriLocator, FakeCommentUriLocator>()
            .AddSingleton<IUriLocator<UserLinkViewModel>, FakeUserLinkLocator>()
            .AddSingleton<AntiforgeryStateProvider, FakeAntiforgeryStateProvider>()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    /// <summary>Токен, как его выдал бы сервер: без него AntiforgeryToken ничего не рисует.</summary>
    private sealed class FakeAntiforgeryStateProvider : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() => new("test-token", "__RequestVerificationToken");
    }

    private sealed class FakeCommentUriLocator : ICommentUriLocator
    {
        public Uri GetCreateCommentUri(ProjectIdentification projectId) => new($"https://test/{projectId.Value}/forums/createcomment");

        public Uri GetConcealCommentUri(ProjectIdentification projectId) => new($"https://test/{projectId.Value}/forums/concealcomment");

        public Uri GetCommentUri(ProjectIdentification projectId, CommentId commentId) => new($"https://test/{projectId.Value}/comment/{commentId.Value}");
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://test/user/{target.UserId?.Value}");
    }
}
