using System.ComponentModel;

namespace JoinRpg.Web.Models;

public class ClaimOperationViewModel
{
    [Required(ErrorMessage = "Заполните текст комментария")]
    [DisplayName("Текст комментария")]
    [UIHint("MarkdownString")]
    public string CommentText { get; set; }

    public string ActionName { get; set; }
}

