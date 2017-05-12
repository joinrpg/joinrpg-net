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

      public IEnumerable<string> LinkTypesToMatch { get; } = new[] {Test};

      public string Render(string match, int index, string extra)
      {
        Assert.AreEqual("%" + Test, match);
        Assert.IsTrue(index > 0);
        return $"<b>{index}</b>{extra}";
      }
    }


    private void NoMatch(string contents)
    {
      var result = new MarkdownString(contents).ToPlainText(_mock);
      Assert.AreEqual(contents, result.ToHtmlString());
    }

    private void Match(string expected, string original)
    {
      var result = new MarkdownString(original).ToHtmlString(_mock);
      Assert.AreEqual(expected, result.ToString());
    }

    private readonly LinkRendererMock _mock = new LinkRendererMock();
    
    [TestMethod]
    public void TestSimpleMatch() => Match("<p><strong>12</strong></p>", "%test12");
    
    [TestMethod]
    public void TestNoMatchWithoutIndex() => NoMatch("%test");

    [TestMethod]
    public void TestNoMatchInMiddle() => NoMatch("test%test12");

    [TestMethod]
    public void TestMatchWithExtra() => Match("<p><strong>121</strong>extra</p>", "%test121(extra)");

    [TestMethod]
    public void TestNoMatchZero() => NoMatch("%test0(extra)");

    [TestMethod]
    public void TestMiddleOfSentence() => Match("<p>s <strong>12</strong></p>", "s %test12");
  }
}