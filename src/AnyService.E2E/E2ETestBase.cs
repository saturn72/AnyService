using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using System;
using Xunit;
using Xunit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using AnyService.SampleApp.Entities;
using AnyService.SampleApp.Models;

namespace AnyService.E2E
{
    public class E2EFixture
    {
        public E2EFixture()
        {
            Factory = new WebApplicationFactory<Startup>();
            Client = Factory.
                WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    MappingExtensions.AddConfiguration(services, "default",
                        cfg =>
                        {
                            cfg.CreateMap<Category, CategoryModel>()
                                .ForMember(dest => dest.CategoryName, mo => mo.MapFrom(src => src.Name));

                            cfg.CreateMap<CategoryModel, Category>()
                            .ForMember(dest => dest.Name, mo => mo.MapFrom(src => src.CategoryName));
                        });
                    var options = new DbContextOptionsBuilder<SampleAppDbContext>()
                          .UseInMemoryDatabase(databaseName: DateTime.Now.ToString("yyyy_mm_hh_ss_ff") + ".db").Options;

                    DbContext = new SampleAppDbContext(options);
                    services.AddTransient<DbContext>(sp => new SampleAppDbContext(options));
                });
            })
            .CreateClient();
        }

        public readonly WebApplicationFactory<Startup> Factory;
        public readonly HttpClient Client;
        public DbContext DbContext;
    }
    [CollectionDefinition(nameof(CollectionName))]
    public class E2ECollection : ICollectionFixture<E2EFixture>
    {
        public const string CollectionName = "e2e-test";
    }
    [Collection(nameof(E2ECollection.CollectionName))]
    public abstract class E2ETestBase
    {
        protected DbContext DbContext;
        protected readonly ITestOutputHelper _output;

        public E2ETestBase(E2EFixture fixture, ITestOutputHelper output)
        {
            _output = output;
            Factory = fixture.Factory;
            HttpClient = fixture.Client;
            DbContext = fixture.DbContext;
        }

        protected void WriteLineToConsole(string line) => _output.WriteLine(line);
        protected WebApplicationFactory<Startup> Factory { get; set; }
        protected HttpClient HttpClient { get; set; }
    }
}