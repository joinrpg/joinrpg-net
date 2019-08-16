using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;
using Autofac;
using Fortis.Erp.Portal.Tests.ContainerTest;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Test.Autofac
{
    /// <summary>
    /// Проверяет, что все джобы правильно зарегистрированы в Autofac и все их зависимости резолвятся
    /// </summary>
    public class AutofacConfigurationTest : IClassFixture<AutofacFixture>
    {
        private readonly IContainer _container;

        public AutofacConfigurationTest(AutofacFixture autofacFixture)
        {
            _container = autofacFixture.Container;
        }

        [Theory]
        [MemberData(nameof(GetControllers))]
        public void AutofacControllers(Type typeToEnsure) => TestType(typeToEnsure);

        public static IEnumerable<object[]> GetControllers()
        {
            return Assembly.GetAssembly(typeof(MvcApplication))
                           .DefinedTypes
                           .Where(type => typeof(Controller).IsAssignableFrom(type))
                           .Where(type => !type.IsAbstract)
                           .Select(type => new[] { type });
        }

        [Theory(Skip = "Need to fix Autofac")]
        [MemberData(nameof(GetApiControllers))]
        public void AutofacApiControllers(Type typeToEnsure) => TestType(typeToEnsure);

        public static IEnumerable<object[]> GetApiControllers()
        {
            return Assembly.GetAssembly(typeof(MvcApplication))
                .DefinedTypes
                .Where(type => typeof(ApiController).IsAssignableFrom(type))
                .Where(type => !type.IsAbstract)
                .Select(type => new[] { type });
        }

        private void TestType(Type type)
        {
            using (var scope = _container.BeginLifetimeScope("AutofacWebRequest"))
            {
                var controller = scope.Resolve(type);

                controller.ShouldNotBeNull();
            }
        }

    }
}
