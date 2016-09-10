using System.Collections.Generic;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;



namespace JoinRpg.Markdown.Test
{
  [TestClass]
  public class LinkRendererTest
  {
    private class LinkRendererMock : ILinkRenderer
    {
      private const string Test = "test";

      #region Implementation of ILinkRenderer

      public IEnumerable<string> LinkTypesToMatch { get; } = new HashSet<string>()
      {
        Test
      };

      public string Render(string match, int index, string extra)
      {
        Assert.AreEqual("@" + Test, match);
        Assert.IsTrue(index > 0);
        return $"{index}.{extra}";
      }

      #endregion
    }


    private void NoMatch(string contents)
    {
      var result = new MarkdownString(contents).ToPlainText(_mock);
      Assert.AreEqual(contents, result);
    }

    private void Match(string expected, string original)
    {
      var result = new MarkdownString(original).ToPlainText(_mock);
      Assert.AreEqual(expected, result);
    }

    private readonly LinkRendererMock _mock = new LinkRendererMock();
    
    [TestMethod]
    public void TestSimpleMatch() => Match("12.", "@test12");

    [TestMethod]
    public void TestNoMatchWithoutIndex() => NoMatch("@test");

    [TestMethod]
    public void TestNoMatchInMiddle() => NoMatch("test@test12");

    [TestMethod]
    public void TestMatchWithExtra() => Match("121.extra", "@test121(extra)");


    [TestMethod]
    public void TestNoMatchZero() => NoMatch("@test0(extra)");
  }
}