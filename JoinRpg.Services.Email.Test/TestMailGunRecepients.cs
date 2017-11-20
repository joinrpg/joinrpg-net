using JoinRpg.DataModel.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;
using JoinRpg.Domain;

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
      var expected = JObject.Parse("{" + string.Join(", ", users.Select(r => $"\"{r.User.Email}\":{{\"name\":\"{r.User.GetDisplayName()}\"}}")) + "}");
      Assert.AreEqual(expected.ToString(), actual.ToString());
    }
  }
}
