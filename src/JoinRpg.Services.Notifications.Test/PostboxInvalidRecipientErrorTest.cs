using JoinRpg.Services.Notifications.Senders.PostboxEmail;

namespace JoinRpg.Services.Notifications.Test;

public class PostboxInvalidRecipientErrorTest
{
    /// <summary>
    /// Текст из инцидента 26.09.2026: Postbox отверг адрес вида g.r.em.grin.6.1.@gmail.com.
    /// </summary>
    [Theory]
    [InlineData("validation failed: invalid “to” addresses")]
    [InlineData("validation failed: invalid \"to\" addresses")]
    public void InvalidToAddress_IsRecognized(string message)
        => PostboxSenderJobService.IsInvalidRecipientError(message).ShouldBeTrue();

    [Theory]
    [InlineData("validation failed: invalid “from” address")]
    [InlineData("Email address is not verified")]
    [InlineData("")]
    public void OtherBadRequest_IsNotRecognized(string message)
        => PostboxSenderJobService.IsInvalidRecipientError(message).ShouldBeFalse();
}
