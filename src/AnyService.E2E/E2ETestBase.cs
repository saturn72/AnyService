using AnyService.SampleApp;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Net.Http;

namespace AnyService.E2E
{
    [TestFixture]
    public class E2ETestBase<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected WebApplicationFactory<TStartup> Factory;

        protected HttpClient Client;


        [OneTimeSetUp]
        public void Init()
        {
            Factory = new WebApplicationFactory<TStartup>();
            Client = Factory.CreateClient();
        }
        [OneTimeTearDown]
        public void TearDown()
        {
            Factory.Dispose();
        }
    }
}
