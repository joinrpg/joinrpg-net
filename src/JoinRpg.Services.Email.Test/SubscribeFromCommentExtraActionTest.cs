using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Services.Email.Test;
public class SubscribeFromCommentExtraActionTest
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<CommentExtraAction>))]
    public void AnyCommentExtraActionTranslated(CommentExtraAction action)
    {
        var predicate = ClaimNotificationService.GetSubscribePredicate(action);
        predicate.ShouldNotBeNull();
        predicate(SubscriptionOptions.CreateAllSet()).ShouldBeTrue();
        predicate(SubscriptionOptions.CreateNoneSet()).ShouldBeFalse();
    }
}
