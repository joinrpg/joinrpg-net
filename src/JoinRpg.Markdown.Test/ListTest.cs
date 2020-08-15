using Xunit;

namespace JoinRpg.Markdown.Test
{

    public class ListTest
    {
        [Fact]
        public void TestListFromSix()
            => @"
6. something
7. something".ShouldBeHtml("<ol start=\"6\">\n<li>something</li>\n<li>something</li>\n</ol>");

        [Fact]
        public void TestBr()
            => @"test
break".ShouldBeHtml("<p>test<br>\nbreak</p>");
    }
}
