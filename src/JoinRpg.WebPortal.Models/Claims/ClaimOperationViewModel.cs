using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public class ClaimOperationViewModel
{
    [Required(ErrorMessage = "Заполните текст комментария")]
    [DisplayName("Текст комментария")]
    [UIHint("MarkdownString")]
    public string CommentText { get; set; }

    public string ActionName { get; set; }
}

