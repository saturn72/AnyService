using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using NUnit.Framework;

namespace AnyService.E2E
{
    [TestFixture]
    public abstract class E2EFixture : WebApplicationFactory<Startup>
    {
        protected WebApplicationFactory<Startup> Factory { get; private set; }
        protected HttpClient HttpClient { get; set; }

        [OneTimeSetUp]
        public void Init()
        {
            Factory = new WebApplicationFactory<Startup>();
        }
    }
}