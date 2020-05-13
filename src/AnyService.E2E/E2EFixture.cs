using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using NUnit.Framework;
using System;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace AnyService.E2E
{
    public abstract class E2EFixture : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly ITestOutputHelper _output;
        private readonly Action<IWebHostBuilder> _configuration;

        public E2EFixture(ITestOutputHelper output, Action<IWebHostBuilder> configuration)
        {
            _output = output;
            _configuration = configuration;
            Factory = new WebApplicationFactory<Startup>();
            HttpClient = Factory.WithWebHostBuilder(builder => _configuration(builder))
            .CreateClient();
        }

        protected void WriteLineToConsole(string line) => _output.WriteLine(line);
        protected WebApplicationFactory<Startup> Factory { get; set; }
        protected HttpClient HttpClient { get; set; }
    }
}