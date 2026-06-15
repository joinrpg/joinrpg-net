using JoinRpg.Services.Notifications.Senders.PostboxEmail;

namespace JoinRpg.Services.Notifications.Tests;

public class FormatAddressTest
{
    [Theory]
    [InlineData("Master", "foo@bar.ru", "Master <foo@bar.ru>")]
    [InlineData("Ivan Ivanov", "x@y.ru", "Ivan Ivanov <x@y.ru>")]
    [InlineData("user@example.com", "user@example.com", "\"user@example.com\" <user@example.com>")]
    [InlineData("Ivanov, Ivan", "x@y.ru", "\"Ivanov, Ivan\" <x@y.ru>")]
    [InlineData("O\"Brien", "x@y.ru", "\"O\\\"Brien\" <x@y.ru>")]
    public void FormatAddress(string displayName, string email, string expected)
    {
        PostboxSenderJobService.FormatAddress(displayName, email).ShouldBe(expected);
    }
}
