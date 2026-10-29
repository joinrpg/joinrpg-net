using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Comments;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Comments;

namespace JoinRpg.WebPortal.Managers.Comments;

/// <summary>
/// Показ обсуждения на странице заявки или темы форума (ADR026, шаги 6 и 8): снимок, модель для
/// текущего пользователя и отметка прочтения — одним вызовом из контроллера.
/// </summary>
/// <remarks>
/// Доступ к обсуждению проверяет вызывающий: у заявки и у темы он устроен по-разному. Построитель
/// модели — деталь сервиса: кроме него, построитель никто не использует.
/// </remarks>
public sealed class DiscussionViewService(
    ICommentDiscussionRepository discussionRepository,
    IUserRepository userRepository,
    IAvatarLoader avatarLoader,
    ICurrentUserAccessor currentUserAccessor,
    ICommentDiscussionService commentDiscussionService)
{
    private readonly DiscussionViewModelBuilder discussionViewModelBuilder =
        new(userRepository, discussionRepository, avatarLoader, currentUserAccessor);

    /// <summary>
    /// Модель обсуждения для текущего пользователя; заодно отмечает всё обсуждение прочитанным.
    /// </summary>
    /// <param name="discussionId">Обсуждение.</param>
    /// <param name="projectInfo">Метаданные проекта владельца обсуждения.</param>
    /// <returns><c>null</c>, если обсуждения нет.</returns>
    public async Task<DiscussionViewModel?> ShowDiscussion(CommentDiscussionId discussionId, ProjectInfo projectInfo)
    {
        var discussion = await discussionRepository.GetDiscussionOrDefault(discussionId);
        if (discussion is null)
        {
            return null;
        }

        // Модель (с пометками «новое» на всём дереве) строится целиком до сдвига отметки прочтения:
        // построитель читает отметку сам, и сдвиг после него на пометки уже не влияет. Иначе и корни,
        // и ответы потеряли бы «новое» уже при первом открытии (ADR026, «Попутно найдено»).
        var model = await discussionViewModelBuilder.Build(discussion, projectInfo);

        // В заблокированном проекте (ADR023) сервис молча не пишет отметку, в архивном — пишет, как и
        // комментарий (ADR026 §5). Непрочитанное ищем среди видимого пользователю: если у игрока новые
        // только скрытые от него комментарии, отметку не трогаем — на счётчики это не влияет, а при
        // первом видимом новом она всё равно уйдёт на последний комментарий.
        if (model.HasUnread())
        {
            await commentDiscussionService.UpdateReadCommentWatermark(
                discussion.Id,
                discussion.Comments.MaxBy(comment => comment.Id.Value)!.Id);
        }

        return model;
    }
}
