using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Test.Infrastructure;

public class ProjectIdExtractorTest
{
    [Fact]
    public void ShouldParsePath() => new PathString("/100/dfsfdfsd").TryExtractFromPath().ShouldBe(new(100));

    [Fact]
    public void ShouldFindIfApi() => new PathString("/x-game-api/100/dfsfdfsd").TryExtractFromPath().ShouldBe(new(100));

    [Fact]
    public void ShouldFindIfWebApi() => new PathString("/webapi/100/dfsfdfsd").TryExtractFromPath().ShouldBe(new(100));

    [Fact]
    public void ShouldNotFindIfNotFirst() => new PathString("/xxx/webapi/100/dfsfdfsd").TryExtractFromPath().ShouldBeNull();

    [Fact]
    public void ShouldNotFoundAfterAnything() => new PathString("/something/100/dfsfdfsd").TryExtractFromPath().ShouldBeNull();

    [Fact]
    public void ShouldIgnoreGarbageAfterApi() => new PathString("/x-game-api/x100/dfsfdfsd").TryExtractFromPath().ShouldBeNull();

    [Fact]
    public void ShouldIgnoreEmpty() => new PathString("").TryExtractFromPath().ShouldBeNull();

    [Fact]
    public void ShouldIgnoreGarbage() => new PathString("/xxx/xxx").TryExtractFromPath().ShouldBeNull();

}
