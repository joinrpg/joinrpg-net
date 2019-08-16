using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Autofac;
using Autofac.Integration.Mvc;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web;
using JoinRpg.Web.App_Start;
using Joinrpg.Web.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace Fortis.Erp.Portal.Tests.ContainerTest
{
    /// <summary>
    /// Allows reuse of Autofac containter between tests
    /// </summary>
    [UsedImplicitly]
    public class AutofacFixture : IDisposable
    {
        public readonly IContainer Container;

        public AutofacFixture()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);


            builder.RegisterModule<WebModule>();

            builder.RegisterType<StubUserTokenProviderFactory>().AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.RegisterType<StubUriService>().AsImplementedInterfaces();
            builder.RegisterType<StubAuthManager>().AsImplementedInterfaces();


            Container = builder.Build();
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }

    /// <summary>
    /// Used because creating DataProtectorTokenProvider requires calling static methods
    /// </summary>
    public class StubUserTokenProviderFactory : IUserTokenProviderFactory
    {
        public DataProtectorTokenProvider<JoinIdentityUser, int> Create() => null;
    }

    public class StubUriService : IUriService
    {
        public string Get(ILinkable link) => throw new NotImplementedException();

        public Uri GetUri(ILinkable link) => throw new NotImplementedException();
    }

    public class StubAuthManager : IAuthenticationManager
    {
        #region Implementation of IAuthenticationManager

        public IEnumerable<AuthenticationDescription> GetAuthenticationTypes() => throw new NotImplementedException();

        public IEnumerable<AuthenticationDescription> GetAuthenticationTypes(Func<AuthenticationDescription, bool> predicate) => throw new NotImplementedException();

        public Task<AuthenticateResult> AuthenticateAsync(string authenticationType) => throw new NotImplementedException();

        public Task<IEnumerable<AuthenticateResult>> AuthenticateAsync(string[] authenticationTypes) => throw new NotImplementedException();

        public void Challenge(AuthenticationProperties properties, params string[] authenticationTypes) => throw new NotImplementedException();

        public void Challenge(params string[] authenticationTypes) => throw new NotImplementedException();

        public void SignIn(AuthenticationProperties properties, params ClaimsIdentity[] identities) => throw new NotImplementedException();

        public void SignIn(params ClaimsIdentity[] identities) => throw new NotImplementedException();

        public void SignOut(AuthenticationProperties properties, params string[] authenticationTypes) => throw new NotImplementedException();

        public void SignOut(params string[] authenticationTypes) => throw new NotImplementedException();

        public ClaimsPrincipal User
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public AuthenticationResponseChallenge AuthenticationResponseChallenge
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public AuthenticationResponseGrant AuthenticationResponseGrant
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public AuthenticationResponseRevoke AuthenticationResponseRevoke
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        #endregion
    }
}
