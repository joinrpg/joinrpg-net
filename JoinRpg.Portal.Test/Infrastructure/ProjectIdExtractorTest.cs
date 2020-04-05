using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

using JoinRpg.Portal.Infrastructure.DiscoverFilters;

namespace JoinRpg.Portal.Test.Infrastructure
{
    public class ProjectIdExtractorTest
    {
        [Fact]
        public void ShouldParsePath()
        {
            new PathString("/100/dfsfdfsd").TryExtractFromPath().ShouldBe(100);
        }

        [Fact]
        public void ShouldIgnoreEmpty()
        {
            new PathString("").TryExtractFromPath().ShouldBeNull();
        }

        [Fact]
        public void ShouldIgnoreGarbage()
        {
            new PathString("/xxx/xxx").TryExtractFromPath().ShouldBeNull();
        }

    }
}
