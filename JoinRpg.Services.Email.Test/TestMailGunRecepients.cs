using JoinRpg.DataModel.Mocks;
using Shouldly;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Linq;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.Services.Interfaces.Email;

namespace JoinRpg.Services.Email.Test
{
  
  public class TestMailGunRecepients
  {

    [Fact]
    public void TestRecepientVariables()
    {
      var mock = new MockedProject();
      var users = new[] { new RecepientData(mock.Player.GetDisplayName(), mock.Player.Email) };
      var expected = JObject.Parse("{" + string.Join(", ", users.Select(r => $"\"{r.Email}\":{{\"name\":\"{r.DisplayName}\"}}")) + "}");
        users.ToRecipientVariables().ShouldBe(expected);
    }
  }
}
