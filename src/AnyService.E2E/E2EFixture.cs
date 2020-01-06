using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using NUnit.Framework;
using System;
using Microsoft.AspNetCore.Hosting;

namespace AnyService.E2E
{
    [TestFixture]
    public abstract class E2EFixture : WebApplicationFactory<Startup>
    {
        private Action<IWebHostBuilder> _configuration;

        public E2EFixture(Action<IWebHostBuilder> configuration)
        {
            _configuration = configuration;
        }
        protected WebApplicationFactory<Startup> Factory { get; set; }
        protected HttpClient HttpClient { get; set; }

        [OneTimeSetUp]
        public void Init()
        {
            Factory = new WebApplicationFactory<Startup>();
            HttpClient = Factory.WithWebHostBuilder(builder => _configuration(builder))
            .CreateClient();
        }
    }
}