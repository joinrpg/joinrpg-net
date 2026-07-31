namespace JoinRpg.Web.Models;

public class ClaimOperationViewModel
{
    [DisplayName("Текст комментария")]
    [UIHint("MarkdownString")]
    public virtual string CommentText { get; set; }

    public string ActionName { get; set; }
}

public class RequiredCommentClaimOperationViewModel : ClaimOperationViewModel
{
    [Required(ErrorMessage = "Заполните текст комментария")]
    [DisplayName("Текст комментария")]
    [UIHint("MarkdownString")]
    public override string CommentText { get; set; }
}

