using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public class CommentTextViewModel
{

    internal static int LastFormIndex = 0;

    public CommentTextViewModel() => FormIndex = Interlocked.Increment(ref LastFormIndex);

    [Required(ErrorMessage = "Заполните текст комментария")]
    [DisplayName("Текст комментария")]
    [UIHint("MarkdownString")]
    public string CommentText { get; set; }

    public bool ShowLabel { get; set; } = true;

    public int FormIndex { get; set; }
}

