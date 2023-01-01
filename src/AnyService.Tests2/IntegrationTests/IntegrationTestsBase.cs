using AnyService.SampleApp;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AnyService.Tests.IntegrationTests
{
    public abstract class IntegrationTestsBase : IClassFixture<WebApplicationFactory<Startup>>
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
