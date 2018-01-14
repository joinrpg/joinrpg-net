using JoinRpg.DataModel.Mocks;
using Shouldly;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Linq;
using JoinRpg.Domain;

namespace JoinRpg.Services.Email.Test
{
  
  public class TestMailGunRecepients
  {

    [Fact]
    public void TestRecepientVariables()
    {
      var mock = new MockedProject();
      var users = new[] { new MailRecipient(mock.Player) };
      var expected = JObject.Parse("{" + string.Join(", ", users.Select(r => $"\"{r.User.Email}\":{{\"name\":\"{r.User.GetDisplayName()}\"}}")) + "}");
        users.ToRecipientVariables().ShouldBe(expected);
    }
  }
}
