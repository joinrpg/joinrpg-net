using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test;

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
