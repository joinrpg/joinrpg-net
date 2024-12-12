using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models;

public class MoveClaimOperationViewModel
{
    [DisplayName("Текст комментария")]
    [UIHint("MarkdownString")]
    public string CommentText { get; set; } = null!;
    [ReadOnly(true)]
    public IList<JoinSelectListItem> PotentialCharactersToMove { get; set; } = [];

    [ReadOnly(true)]
    public bool IsAlreadyAccepted { get; set; }

    [ReadOnly(true)]
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public int CharacterId { get; set; }

    public bool CanAcceptAfter { get; set; }

    [Display(Name = "Принять заявку", Description = "После перемещения на нового персонажа заявку будет сразу принята")]
    public bool AcceptAfterMove { get; set; }
}

