using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.Portal.Controllers.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Portal.Test.Controllers.Common;

public class JoinMvcControllerBaseTest
{
    // Регрессия: JoinRpgSlotLimitedException (переполнение слота при одобрении заявки мастером —
    // ожидаемая бизнес-ситуация) должна показываться игроку понятным сообщением и не логироваться
    // как ошибка, а не проваливаться в default-ветку с LogError и "Неожиданная ошибка".
    [Fact]
    public void AddModelException_SlotLimitedException_ShouldAddFriendlyErrorWithoutLoggingError()
    {
        var mock = new MockedProject();
        var logger = new RecordingLogger();
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(logger),
            },
        };

        controller.CallAddModelException(new JoinRpgSlotLimitedException(mock.Character));

        controller.ModelState.IsValid.ShouldBeFalse();
        var error = controller.ModelState[""]!.Errors.ShouldHaveSingleItem();
        error.ErrorMessage.ShouldBe("Не удалось принять заявку: свободные места на эту роль закончились");
        logger.ErrorCount.ShouldBe(0);
    }

    // Регрессия: OnlyOneApprovedClaimException (у игрока уже есть одобренная заявка, а проект
    // допускает только одного персонажа на игрока — ожидаемая бизнес-ситуация) должна показываться
    // понятным сообщением и не логироваться как ошибка, а не проваливаться в default-ветку
    // с LogError и "Неожиданная ошибка". См. #4691.
    [Fact]
    public void AddModelException_OnlyOneApprovedClaimException_ShouldAddFriendlyErrorWithoutLoggingError()
    {
        var logger = new RecordingLogger();
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(logger),
            },
        };

        controller.CallAddModelException(new OnlyOneApprovedClaimException());

        controller.ModelState.IsValid.ShouldBeFalse();
        var error = controller.ModelState[""]!.Errors.ShouldHaveSingleItem();
        error.ErrorMessage.ShouldBe("Заявка не принята: у игрока уже есть одобренная заявка на другого персонажа в этом проекте");
        logger.ErrorCount.ShouldBe(0);
    }

    private static DefaultHttpContext CreateHttpContext(ILogger<JoinMvcControllerBase> logger)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new TestServiceProvider().WithService(logger),
        };
        return httpContext;
    }

    private sealed class TestController : JoinMvcControllerBase
    {
        public void CallAddModelException(Exception exception) => AddModelException(exception);
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> services = new();

        public TestServiceProvider WithService<T>(T service) where T : notnull
        {
            services[typeof(T)] = service;
            return this;
        }

        public object? GetService(Type serviceType) => services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private sealed class RecordingLogger : ILogger<JoinMvcControllerBase>
    {
        public int ErrorCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                ErrorCount++;
            }
        }
    }
}
