using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace AnyService.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        public class MyClass : IEntity
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
        [Fact]
        public void HasDuplicatedEntityConfigRecords()
        {
            var c = new AnyServiceConfig
            {
                EntityConfigRecords = new[]
                {
                    new EntityConfigRecord { Type = typeof(MyClass) },
                    new EntityConfigRecord { Type = typeof(MyClass) },
                }
            };
            var sc = new Mock<IServiceCollection>();
            Should.Throw<InvalidOperationException>(() => ServiceCollectionExtensions.AddAnyService(sc.Object, c));
        }
    }
}
