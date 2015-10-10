using System;
using JoinRpg.Services.Interfaces;
using JoinRpg.TestHelpers;
using JoinRpg.Web.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Web.Test
{
  [TestClass]
  public class ProblemEnumTest
  {
    [TestMethod]
    public void ProblemEnum()
    {
      EnumerationTestHelper.CheckEnums<ClaimProblemType, ProblemTypeViewModel>();
    }
  }
}
