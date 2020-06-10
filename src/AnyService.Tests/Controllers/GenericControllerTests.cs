using System.Linq;
using System.Reflection;
using AnyService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Controllers
{
    public class GenericControllerTests
    {
        [Fact]
        public void ValidateRoute()
        {
            var type = typeof(GenericController<>);
            var route = type.GetCustomAttributes(typeof(RouteAttribute)).First() as RouteAttribute;
            route.Template.ShouldBe("[controller]");
        }
        [Theory]
        [InlineData(nameof(GenericController<MyClass>.Post), "POST", null)]
        [InlineData(nameof(GenericController<MyClass>.PostMultipart), "POST", "__multipart")]
        [InlineData(nameof(GenericController<MyClass>.PostMultipartStream), "POST", "__stream")]
        [InlineData(nameof(GenericController<MyClass>.PutMultipartStream), "PUT", "__stream/{id}")]
        [InlineData(nameof(GenericController<MyClass>.GetAll), "GET", null)]
        [InlineData(nameof(GenericController<MyClass>.Get), "GET", "{id}")]
        [InlineData(nameof(GenericController<MyClass>.Put), "PUT", "{id}")]
        public void ValidateVerbs(string methodName, string expHttpVerb, string expTemplate)
        {
            var type = typeof(GenericController<>);
            var mi = type.GetMethod(methodName);
            var att = mi.GetCustomAttributes(typeof(HttpMethodAttribute)).First() as HttpMethodAttribute;
            att.HttpMethods.First().ShouldBe(expHttpVerb);
            att.Template.ShouldBe(expTemplate);
        }
        public class MyClass : IDomainModelBase
        {
            public string Id { get; set; }
        }
    }
}