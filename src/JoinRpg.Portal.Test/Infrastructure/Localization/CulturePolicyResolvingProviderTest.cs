using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using JoinRpg.Portal.Infrastructure.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace JoinRpg.Portal.Test.Infrastructure.Localization
{
    public class CulturePolicyResolvingProviderTest
    {
        private readonly CulturePolicyResolvingProvider _provider = new CulturePolicyResolvingProvider();

        [Fact]
        public async void DetermineProviderCultureResult_WithNullContext_ShouldThrowArgumentExceptionOnNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.DetermineProviderCultureResult(null));
        }

        [Fact]
        public void DetermineProviderCultureResult_ShouldSetCultureFromRequestParameterIfItIsSupported()
        {
            //arrange
            var culture = "de-DE";

            var query = new Dictionary<string, string>();
            query.Add("culture", culture);

            var cookies = new Dictionary<string, string>();
            var cookie = "c=ru-RU|uic=ru-RU";
            cookies.Add(CookieRequestCultureProvider.DefaultCookieName, cookie);

            var httpRequest = new TestHttpRequest(query, cookies);
            var httpContext = new TestHttpContext(httpRequest);

            httpRequest.Headers.Add("Accept-Language", "en-US, en;q=0.8, fi;q=0.7, *;q=0.5");

            var expected = Task.FromResult(new ProviderCultureResult(culture));
            //action
            var actual = _provider.DetermineProviderCultureResult(httpContext);
            //assert
            Assert.Equal(expected.Result.Cultures, actual.Result.Cultures);
        }

        [Fact]
        public void DetermineProviderCultureResult_ShouldSetCultureFromCookieIfItIsSupported()
        {
            //arrange
            var expected = Task.FromResult(new ProviderCultureResult("ru-RU"));

            var cookies = new Dictionary<string, string>();
            var cookie = "c=ru-RU|uic=ru-RU";
            cookies.Add(CookieRequestCultureProvider.DefaultCookieName, cookie);

            var httpRequest = new TestHttpRequest(new Dictionary<string, string>(), cookies);
            var httpContext = new TestHttpContext(httpRequest);

            httpRequest.Headers.Add("Accept-Language", "de-DE, de;q=0.8, fr;q=0.7, *;q=0.5");
            //action
            var actual = _provider.DetermineProviderCultureResult(httpContext);
            //assert
            Assert.Equal(expected.Result.Cultures, actual.Result.Cultures);
        }

        [Fact]
        public void DetermineProviderCultureResult_ShouldSetCultureFromHeaderIfItIsSupported()
        {
            //arrange
            var httpRequest = new TestHttpRequest(new Dictionary<string, string>(), new Dictionary<string, string>());
            var httpContext = new TestHttpContext(httpRequest);

            httpRequest.Headers.Add("Accept-Language", "en-US, en;q=0.8, de;q=0.7, *;q=0.5");
            var expected = Task.FromResult(new ProviderCultureResult("en"));
            //action
            var actual = _provider.DetermineProviderCultureResult(httpContext);
            //assert
            Assert.Equal(expected.Result.Cultures, actual.Result.Cultures);
        }

        [Fact]
        public void DetermineProviderCultureResult_ShouldSetCultureFromHeaderIfCookieContainsMalformedString()
        {
            //arrange
            var cookies = new Dictionary<string, string>();
            var cookie = "c=ru-RU///uic=ru-RU";
            cookies.Add(CookieRequestCultureProvider.DefaultCookieName, cookie);
            var httpRequest = new TestHttpRequest(new Dictionary<string, string>(), cookies);
            var httpContext = new TestHttpContext(httpRequest);

            httpRequest.Headers.Add("Accept-Language", "en-US, en;q=0.8, de;q=0.7, *;q=0.5");
            var expected = Task.FromResult(new ProviderCultureResult("en"));
            //action
            var actual = _provider.DetermineProviderCultureResult(httpContext);
            //assert
            Assert.Equal(expected.Result.Cultures, actual.Result.Cultures);
        }

        [Fact]
        public void DetermineProviderCultureResult_ShouldSetCultureFromHeaderIfCookieValuesAreMalformed()
        {
            //arrange
            var cookies = new Dictionary<string, string>();
            var cookie = "c!!!ru-RU|uic///ru-RU";
            cookies.Add(CookieRequestCultureProvider.DefaultCookieName, cookie);
            var httpRequest = new TestHttpRequest(new Dictionary<string, string>(), cookies);
            var httpContext = new TestHttpContext(httpRequest);

            httpRequest.Headers.Add("Accept-Language", "en-US, en;q=0.8, de;q=0.7, *;q=0.5");
            var expected = Task.FromResult(new ProviderCultureResult("en"));
            //action
            var actual = _provider.DetermineProviderCultureResult(httpContext);
            //assert
            Assert.Equal(expected.Result.Cultures, actual.Result.Cultures);
        }

        [Fact]
        public void DetermineProviderCultureResult_ShouldSetCultureFromHeaderIfCookieValueIsEmpty()
        {
            //arrange
            var cookies = new Dictionary<string, string>();
            var cookie = "";
            cookies.Add(CookieRequestCultureProvider.DefaultCookieName, cookie);
            var httpRequest = new TestHttpRequest(new Dictionary<string, string>(), cookies);
            var httpContext = new TestHttpContext(httpRequest);

            httpRequest.Headers.Add("Accept-Language", "en-US, en;q=0.8, de;q=0.7, *;q=0.5");
            var expected = Task.FromResult(new ProviderCultureResult("en"));
            //action
            var actual = _provider.DetermineProviderCultureResult(httpContext);
            //assert
            Assert.Equal(expected.Result.Cultures, actual.Result.Cultures);
        }

        [Fact]
        public void DetermineProviderCultureResult_ShouldReturnNullIfRequestParameterCookieAndHeaderAreEmptyOrMalformed()
        {
            //arrange
            var culture = "fi";

            var query = new Dictionary<string, string>();
            query.Add("culture", culture);

            var cookies = new Dictionary<string, string>();
            var cookie = "cr\\u-RU";
            cookies.Add(CookieRequestCultureProvider.DefaultCookieName, cookie);

            var httpRequest = new TestHttpRequest(query, cookies);
            var httpContext = new TestHttpContext(httpRequest);

            httpRequest.Headers.Add("Accept-Language", "en-USmalformed=0.5");

            var expected = Task.FromResult(new ProviderCultureResult("ru-RU"));
            //action
            var actual = _provider.DetermineProviderCultureResult(httpContext);
            //assert
            Assert.Equal(expected.Result.Cultures, actual.Result.Cultures);
        }

        private class TestHeaderDictionary : IHeaderDictionary
        {
            private Dictionary<string, string> _headers;
            public TestHeaderDictionary(Dictionary<string, string> from)
            {
                _headers = new Dictionary<string, string>(from);
            }
            public StringValues this[string key] { get => _headers[key]; set => _headers[key] = value; }

            public long? ContentLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ICollection<string> Keys => throw new NotImplementedException();

            public ICollection<StringValues> Values => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool IsReadOnly => throw new NotImplementedException();

            public void Add(string key, StringValues value)
            {
                _headers.Add(key, value);
            }

            public void Add(KeyValuePair<string, StringValues> item)
            {
                _headers.Add(item.Key, item.Value);
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<string, StringValues> item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(string key)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool Remove(string key)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<string, StringValues> item)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out StringValues value)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
        private class TestCookieCollection : IRequestCookieCollection
        {
            private Dictionary<string, string> _cookies;

            public TestCookieCollection(Dictionary<string, string> cookies)
            {
                _cookies = cookies;
            }

            public string this[string key] => _cookies[key];

            public int Count => throw new NotImplementedException();

            public ICollection<string> Keys => throw new NotImplementedException();

            public bool ContainsKey(string key)
            {
                return _cookies.ContainsKey(key);
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out string value)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
        private class TestQueryCollection : IQueryCollection
        {
            private Dictionary<string, string> _query = new Dictionary<string, string>();

            public TestQueryCollection(Dictionary<string, string> from)
            {
                _query = new Dictionary<string, string>(from);
            }

            public StringValues this[string key] => _query[key];

            public int Count => throw new NotImplementedException();

            public ICollection<string> Keys => throw new NotImplementedException();

            public bool ContainsKey(string key)
            {
                return _query.ContainsKey(key);
            }

            public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out StringValues value)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private class TestHttpRequest : HttpRequest
        {

            private IQueryCollection _query;
            private IRequestCookieCollection _cookies;
            private IHeaderDictionary _headers;

            public TestHttpRequest(Dictionary<string, string> query,
                Dictionary<string, string> cookies)
            {
                _headers = new TestHeaderDictionary(new Dictionary<string, string>());
                _query = new TestQueryCollection(query);
                _cookies = new TestCookieCollection(cookies);
            }

            public override Stream Body { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override long? ContentLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override IRequestCookieCollection Cookies { get => _cookies; set => _cookies = value; }
            public override IFormCollection Form { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override bool HasFormContentType => throw new NotImplementedException();

            public override IHeaderDictionary Headers => _headers;

            public override HostString Host { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override HttpContext HttpContext => throw new NotImplementedException();

            public override bool IsHttps { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override string Method { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override PathString Path { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override PathString PathBase { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override string Protocol { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override IQueryCollection Query { get => _query; set => _query = value; }
            public override QueryString QueryString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override string Scheme { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

        private class TestHttpContext : HttpContext
        {
            private HttpRequest _request;

            public TestHttpContext(HttpRequest request)
            {
                _request = request;
            }
            public override ConnectionInfo Connection => throw new NotImplementedException();

            public override IFeatureCollection Features => throw new NotImplementedException();

            public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override HttpRequest Request => _request;

            public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override HttpResponse Response => throw new NotImplementedException();

            public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override WebSocketManager WebSockets => throw new NotImplementedException();

            public override void Abort()
            {
                throw new NotImplementedException();
            }
        }


    }
}
