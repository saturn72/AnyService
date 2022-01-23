using Microsoft.Extensions.FileProviders;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace AnyService.Core.Tests
{
    public class AppDomainTypeFinderTests
    {
        [Fact]
        public void DefaultValues()
        {
            AppDomainTypeFinder.EXCLUDED_ASSEMBLIES_PATTERN.ShouldBe("^AutoMapper|^Castle|^coverlet|^EasyCaching|^LiteDb|^Microsoft|^Moq|^mscorlib|^Newtonsoft.Json|^Nuget|^RabbitMQ|^Shouldly|^System|^xunit");
            AppDomainTypeFinder.TO_ONLY_LOAD.ShouldBe(".*");
        }

        public interface IMyInterface
        {
            int MyProperty { get; set; }
        }
        public class MyClass : IMyInterface
        {
            public int MyProperty { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        }
        [Fact]
        public void GetObjectsOfType()
        {
            var fp = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory);
            var finder = new AppDomainTypeFinder(fp);
            var types = finder.FindAllTypesOf<IMyInterface>().ToArray();
            types.Length.ShouldBe(1);
            typeof(IMyInterface).IsAssignableFrom(types.FirstOrDefault()).ShouldBeTrue();
        }
    }
}
