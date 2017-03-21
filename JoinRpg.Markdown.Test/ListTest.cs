﻿using System;
using System.Collections.Generic;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Markdown.Test
{
  [TestClass]
  public class ListTest
  {
    [TestMethod]
    public void TestListFromSix()
      => TestMarkdown(
@"
6. something
7. something",
@"<ol start=""6"">
<li>something</li>
<li>something</li>
</ol>");

    [TestMethod]
    public void TestBr()
      => TestMarkdown(
@"test
break",
"<p>test<br>\nbreak</p>");

    private void TestMarkdown(string markdown, string expectedHtml)
    {
      var actualHtml = new MarkdownString(markdown).ToHtmlString().ToHtmlString();
      Assert.AreEqual(expectedHtml, actualHtml);
    }
  }
}