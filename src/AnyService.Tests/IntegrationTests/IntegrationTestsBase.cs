using AnyService.SampleApp;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using Xunit;

namespace AnyService.Tests.IntegrationTests
{
    public abstract class IntegrationTestsBase
        : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        protected HttpClient Client;
        public IntegrationTestsBase(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            Client = _factory.CreateClient();
        }
    }
}
