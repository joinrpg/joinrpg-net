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
      var users = new[] { mock.Player };
      var actual = users.ToRecepientVariables();
      var expected = JObject.Parse("{" + string.Join(", ", users.Select(r => $"\"{r.Email}\":{{\"name\":\"{r.DisplayName}\"}}")) + "}");
      Assert.AreEqual(expected.ToString(), actual.ToString());
    }
  }
}
