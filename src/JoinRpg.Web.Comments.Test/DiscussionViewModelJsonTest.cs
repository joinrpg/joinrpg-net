using System.Text.Json;
using static JoinRpg.Web.Comments.Test.CommentTestData;

namespace JoinRpg.Web.Comments.Test;

/// <summary>
/// Модель обсуждения сериализуема (ADR026 §3): её можно отдать острову или WASM без переделки.
/// </summary>
public class DiscussionViewModelJsonTest
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DiscussionSurvivesRoundTrip()
    {
        var withFinance = Comment(2, isVisibleToPlayer: false, isRead: false, depth: 1) with
        {
            ExtraActionText = "Оплачен взнос",
            Finance = new CommentFinanceViewModel("Оплата денег игроком", "Оплата", 500, new DateOnly(2026, 10, 2), "Наличными — Мастер", IsDeclined: false, IsProposed: true, CanModerate: true),
        };
        var original = Discussion(
            hasMasterAccess: true,
            Comment(1, canConceal: true, replies: [withFinance]) with { AuthorAvatarUri = null },
            Comment(3) with { Author = UserLinkViewModel.Deleted });

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DiscussionViewModel>(json, Options);

        restored.ShouldNotBeNull();
        // Record'ы со списками сравниваются по ссылке, а ShouldBeEquivalentTo спотыкается о тип
        // коллекции (массив против List) — сверяем повторную сериализацию и значения вложенных record'ов.
        JsonSerializer.Serialize(restored, Options).ShouldBe(json);
        restored.RootComments[0].Replies.ShouldHaveSingleItem().Finance.ShouldBe(withFinance.Finance);
        restored.RootComments[1].Author.ShouldBe(UserLinkViewModel.Deleted);
    }
}
