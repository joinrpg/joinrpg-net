using System.Text.Json;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Web.AdminTools;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test.Infrastructure;

public class WebApiExceptionHandlerTest
{
    private static readonly ProjectIdentification ProjectId = new(1611);

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        return new DefaultHttpContext
        {
            RequestServices = services,
            Request = { Path = path },
            Response = { Body = new MemoryStream() },
        };
    }

    private static async Task<(bool Handled, JsonElement Problem)> Handle(HttpContext httpContext, Exception exception)
    {
        var handler = new WebApiExceptionHandler(httpContext.RequestServices.GetRequiredService<IProblemDetailsService>());
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        httpContext.Response.Body.Position = 0;
        var problem = handled ? JsonDocument.Parse(httpContext.Response.Body).RootElement : default;
        return (handled, problem);
    }

    [Fact]
    public async Task BlockedProject_InWebApi_ReturnsConflictWithReason()
    {
        var httpContext = CreateHttpContext("/webapi/1611/project/SaveContactSettings");

        var (handled, problem) = await Handle(httpContext, new ProjectBlockedException(ProjectId));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");
        problem.GetProperty("title").GetString().ShouldBe("Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны");
    }

    [Fact]
    public async Task ProjectNotFound_InWebApi_ReturnsNotFoundWithReason()
    {
        var httpContext = CreateHttpContext("/webapi/1611/project/SaveContactSettings");

        var (handled, problem) = await Handle(httpContext, new ProjectNotFoundException(ProjectId));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problem.GetProperty("title").GetString().ShouldBe("Проект не найден");
    }

    // Остров настроек заявок показывает причину, если шаблоном по умолчанию выбран закрытый для игроков (#3303).
    [Fact]
    public async Task DefaultTemplateMustAcceptClaims_InWebApi_ReturnsConflictWithReason()
    {
        var httpContext = CreateHttpContext("/webapi/1611/project/SaveClaimSettings");
        var exception = new DefaultTemplateMustAcceptClaimsException(new CharacterIdentification(ProjectId, 5));

        var (handled, problem) = await Handle(httpContext, exception);

        handled.ShouldBeTrue();
        WebApiExceptionHandler.IsExpectedRefusal(exception).ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("title").GetString().ShouldStartWith("Шаблон по умолчанию должен принимать заявки игроков");
    }

    [Fact]
    public async Task ParentCommentNotFound_InWebApi_ReturnsNotFoundWithReason()
    {
        var httpContext = CreateHttpContext("/webapi/claim-comment/AddComment");

        var (handled, problem) = await Handle(httpContext, new ParentCommentNotFoundException(12345));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problem.GetProperty("title").GetString().ShouldStartWith("Комментарий, на который вы отвечаете, не найден");
    }

    // Отказ Identity при смене email в админке — штатный, не 500 (#5891)
    [Fact]
    public async Task UserAdminActionRefused_InWebApi_ReturnsBadRequestWithMessage()
    {
        var httpContext = CreateHttpContext("/webapi/UserAdmin/ChangeEmail");

        var (handled, problem) = await Handle(httpContext, new UserAdminActionRefusedException("Этот email уже используется другим пользователем"));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.GetProperty("title").GetString().ShouldBe("Этот email уже используется другим пользователем");
        WebApiExceptionHandler.IsExpectedRefusal(new UserAdminActionRefusedException("")).ShouldBeTrue();
    }

    [Fact]
    public async Task VisibleReplyToHiddenComment_InWebApi_ReturnsConflictWithReason()
    {
        var httpContext = CreateHttpContext("/webapi/claim-comment/AddComment");

        var (handled, problem) = await Handle(httpContext, new VisibleReplyToHiddenCommentException(12345));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("title").GetString().ShouldStartWith("Ответ на комментарий, скрытый от игрока");
    }

    // Остров настроек полей показывает причину, если выбрать непубличное поле имени персонажа (#965)
    [Fact]
    public async Task NameFieldShouldBePublic_InWebApi_ReturnsConflictWithReason()
    {
        var httpContext = CreateHttpContext("/webapi/1611/project/SaveFieldSettings");
        var exception = new JoinRpgNameFieldShouldBePublicException(new ProjectField { ProjectId = ProjectId.Value, FieldName = "Имя персонажа" });

        var (handled, problem) = await Handle(httpContext, exception);

        handled.ShouldBeTrue();
        WebApiExceptionHandler.IsExpectedRefusal(exception).ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("title").GetString().ShouldStartWith("Поле, привязанное к имени персонажа, должно быть публичным");
    }

    [Fact]
    public async Task UnknownException_InWebApi_ReturnsServerErrorProblem()
    {
        var httpContext = CreateHttpContext("/webapi/1611/project/SaveContactSettings");

        var (handled, problem) = await Handle(httpContext, new InvalidOperationException("секрет"));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problem.GetProperty("title").GetString().ShouldBe("Неизвестная ошибка на сервере");
        problem.GetRawText().ShouldNotContain("секрет");
    }

    /// <summary>
    /// Так обработчик вызывает <c>UseExceptionHandler</c> со страницей ошибки: путь запроса уже переписан на
    /// <c>/error</c>, исходный — в <see cref="IExceptionHandlerPathFeature"/>.
    /// </summary>
    [Fact]
    public async Task WebApiPathRewrittenToErrorPage_StillHandled()
    {
        var httpContext = CreateHttpContext("/error");
        httpContext.Features.Set<IExceptionHandlerPathFeature>(
            new ExceptionHandlerFeature { Path = "/webapi/1611/project/SaveContactSettings", Error = new ProjectBlockedException(ProjectId) });

        var (handled, problem) = await Handle(httpContext, new ProjectBlockedException(ProjectId));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("title").GetString().ShouldStartWith("Проект заблокирован");
    }

    [Fact]
    public async Task PageRewrittenToErrorPage_NotHandled()
    {
        var httpContext = CreateHttpContext("/error");
        httpContext.Features.Set<IExceptionHandlerPathFeature>(
            new ExceptionHandlerFeature { Path = "/1611/project/settings", Error = new ProjectBlockedException(ProjectId) });

        var (handled, _) = await Handle(httpContext, new ProjectBlockedException(ProjectId));

        handled.ShouldBeFalse();
    }

    [Fact]
    public async Task OutsideWebApi_NotHandled()
    {
        var httpContext = CreateHttpContext("/1611/project/settings");

        var (handled, _) = await Handle(httpContext, new ProjectBlockedException(ProjectId));

        handled.ShouldBeFalse();
    }

    [Fact]
    public void UnknownException_IsNotExpectedRefusal()
    {
        WebApiExceptionHandler.IsExpectedRefusal(new InvalidOperationException()).ShouldBeFalse();
        WebApiExceptionHandler.IsExpectedRefusal(new ProjectDeactivatedException(ProjectId)).ShouldBeTrue();
    }
}
