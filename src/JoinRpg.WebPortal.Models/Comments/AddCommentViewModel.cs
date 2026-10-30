namespace JoinRpg.Web.Models;

public class AddCommentViewModel : CommentTextViewModel, IValidatableObject
{
    public int ProjectId { get; set; }

    /// <summary>
    /// Обсуждение, в которое пишем. Постит только форма комментария (<c>AddCommentForm</c> в
    /// <c>JoinRpg.Web.Comments</c>) в <c>ForumController.CreateComment</c>; финансовые формы,
    /// унаследованные от этой модели, его не передают — у них <c>null</c>.
    /// </summary>
    public CommentDiscussionId? CommentDiscussionId { get; set; }

    /// <summary>
    /// Комментарий, на который отвечаем; <c>null</c> — комментарий верхнего уровня.
    /// </summary>
    public CommentId? ParentCommentId { get; set; }

    [DisplayName("Только другим мастерам")]
    public bool HideFromUser { get; set; }

    public bool EnableHideFromUser { get; set; }

    public string ActionName { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HideFromUser && !EnableHideFromUser)
        {
            yield return new ValidationResult("Нельзя скрыть данный комментарий от игрока");
        }
    }
}

