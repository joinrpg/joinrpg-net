using JoinRpg.DataModel.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace JoinRpg.Services.Email.Test
{
  [TestClass]
  public class TestMailGunRecepients
  {

    [TestMethod]
    public void TestRecepientVariables()
    {
      var mock = new MockedProject();
      var users = new[] { new MailRecipient(mock.Player) };
      var actual = users.ToRecipientVariables();
      var expected = JObject.Parse("{" + string.Join(", ", users.Select(r => $"\"{r.User.Email}\":{{\"name\":\"{r.User.DisplayName}\"}}")) + "}");
      Assert.AreEqual(expected.ToString(), actual.ToString());
    }
  }
}
