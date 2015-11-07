using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Helpers.Test
{
  [TestClass]
  public class ExpressionHelpers
  {
    // ReSharper disable once ClassNeverInstantiated.Local
    private class AnonClass
    {
      // ReSharper disable once UnusedAutoPropertyAccessor.Local
      public int Prop { get; set; }
    }

    [TestMethod]
    public void TestAsPropertyName()
    {
      Expression<Func<AnonClass,int>> lambda = foo => foo.Prop;

      Assert.AreEqual("Prop", lambda.AsPropertyName());
    }
  }
}
