using AnyService.SampleApp;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Net.Http;

namespace AnyService.E2E
{
    [TestFixture]
    public class WebApplicationFactoryFixture : WebApplicationFactory<Startup>
    {
        protected WebApplicationFactory<Startup> Factory;

        protected HttpClient Client;


        [OneTimeSetUp]
        public void Init()
        {
            Factory = new WebApplicationFactory<Startup>();
            Client = Factory.CreateClient();
        }
        [OneTimeTearDown]
        public void TearDown()
        {
            Factory.Dispose();
        }
    }
}
