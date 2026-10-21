using JoinRpg.Portal.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test.Infrastructure;

/// <summary>
/// Клиенты островов показывают Title из ProblemDetails как есть (#5735), поэтому голый <c>BadRequest()</c>
/// у <c>[ApiController]</c> не должен приносить английское «Bad Request».
/// </summary>
public class WebApiClientErrorTitlesTest
{
    private static ProblemDetails CreateProblem(int status)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddControllers().Services
            .Configure<ApiBehaviorOptions>(WebApiClientErrorTitles.Configure)
            .BuildServiceProvider();
        var factory = services.GetRequiredService<ProblemDetailsFactory>();
        return factory.CreateProblemDetails(new DefaultHttpContext { RequestServices = services }, status);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, "Некорректный запрос")]
    [InlineData(StatusCodes.Status403Forbidden, "Недостаточно прав для этого действия")]
    [InlineData(StatusCodes.Status404NotFound, "Не найдено")]
    public void ClientError_HasRussianTitle(int status, string title)
        => CreateProblem(status).Title.ShouldBe(title);

    [Fact]
    public void ClientError_KeepsLink()
        => CreateProblem(StatusCodes.Status404NotFound).Type.ShouldNotBeNullOrEmpty();
}
