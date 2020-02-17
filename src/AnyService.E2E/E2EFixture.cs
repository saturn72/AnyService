using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using NUnit.Framework;
using System;
using Microsoft.AspNetCore.Hosting;
using LiteDB;
using System.IO;
using System.Threading;

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

            //remove db if exists on creation
            var dbFile = "anyservice-testsapp.db";
            if (File.Exists(dbFile))
            {
                using var db = new LiteDatabase(dbFile);
                var colNames = db.GetCollectionNames();
                foreach (var name in colNames)
                    db.DropCollection(name);
                Thread.Sleep(1000);
            }
        }
    }
}