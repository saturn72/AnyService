using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using System;
using Xunit;
using Xunit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnyService.E2E
{
    public abstract class E2EFixture : IClassFixture<WebApplicationFactory<Startup>>
    {
        protected DbContext DbContext;
        protected readonly ITestOutputHelper _output;

        public E2EFixture(ITestOutputHelper output)
        {
            _output = output;
            Factory = new WebApplicationFactory<Startup>();

            HttpClient = Factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var options = new DbContextOptionsBuilder<SampleAppDbContext>()
                          .UseSqlite("Filename=:memory:").Options;

                    DbContext = new SampleAppDbContext(options);
                    services.AddTransient<DbContext>(sp => new SampleAppDbContext(options));
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    });
                });
            })
            .CreateClient();
        }

        protected void WriteLineToConsole(string line) => _output.WriteLine(line);
        protected WebApplicationFactory<Startup> Factory { get; set; }
        protected HttpClient HttpClient { get; set; }
    }
}