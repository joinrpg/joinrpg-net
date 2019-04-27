using System;
using Autofac;
using Shouldly;
using Xunit;

namespace JoinRpg.Portal.Test.ContainterTest
{
    /// <summary>
    /// Проверяет, что все контроллеры правильно зарегистрированы в Autofac и все их зависимости резолвятся
    /// </summary>
    public class AutofacResolveTest : IClassFixture<AutofacFixture>
    {
        private readonly IContainer container;

        public AutofacResolveTest(AutofacFixture autofacFixture)
        {
            container = autofacFixture.Container;
        }

        [Theory]
        [ClassData(typeof(ControllerDataSource))]
        public void AutofacControllers(Type typeToEnsure)
        {
            container.Resolve(typeToEnsure).ShouldNotBeNull();
        }
    }
}
