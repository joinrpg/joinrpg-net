using System.Linq;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

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
