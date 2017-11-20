using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Domain.Test
{
    [TestClass]
    public class UserExtensionsTest
    {
        [TestMethod]
        public void UserNameWithoutPrefferedName()
        {
            var user = new User
            {
                Email = "somebody@example.com"
            };
            Assert.AreEqual("somebody", user.GetDisplayName());
        }

    }
}
