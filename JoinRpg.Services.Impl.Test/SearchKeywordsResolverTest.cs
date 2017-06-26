using JoinRpg.Services.Impl.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Services.Impl.Test
{
  [TestClass]
  public class SearchKeywordsResolverTest
  {
    private static readonly string[] keysForPerfectMath =
    {
      "%контакты",
      "контакты"
    };

    private void Verify(string searchString, int? expectedId, bool exactMatchFlag)
    {
      bool isPerfectMatch;
      int? id = SearchKeywordsResolver.TryGetId(searchString, keysForPerfectMath, out isPerfectMatch);

      Assert.AreEqual(expectedId, id, $"ExpectedId was wrong for {searchString}");
      Assert.AreEqual(exactMatchFlag, isPerfectMatch, $"isPerfectMathc was wrong for {searchString}");
    }

    [TestMethod]
    public void VerifyEmptyString()
    {
      Verify(
        "",
        expectedId: null,
        exactMatchFlag: false);
    }

    [TestMethod]
    public void VerifyBareNumber()
    {
      Verify(
        "123",
        expectedId: 123,
        exactMatchFlag: false);
    }

    [TestMethod]
    public void VerifyBareNumberWithSpaces()
    {
      Verify(
        " 123 ",
        expectedId: 123,
        exactMatchFlag: false);
    }

    [TestMethod]
    public void VerifyKeyword()
    {
      Verify(
        "%контакты123",
        expectedId: 123,
        exactMatchFlag: true);
    }

    [TestMethod]
    public void VerifyKeywordWrongCase()
    {
      Verify(
        "КОнТаКтЫ321",
        expectedId: 321,
        exactMatchFlag: true);
    }

    [TestMethod]
    public void VerifySpaceDelimeter()
    {
      Verify(
        "%контакты 123",
        expectedId: null,
        exactMatchFlag: false);
    }
  }
}
