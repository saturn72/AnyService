using System.Linq;
using System.Reflection;
using AnyService.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Controllers
{
    public class GenericControllerTests
    {
        [Theory]
        [InlineData(nameof(GenericController<MyClass, MyClass>.Post), "POST", null)]
        [InlineData(nameof(GenericController<MyClass, MyClass>.PostMultipart), "POST", "__multipart")]
        [InlineData(nameof(GenericController<MyClass, MyClass>.PostMultipartStream), "POST", "__stream")]
        [InlineData(nameof(GenericController<MyClass, MyClass>.PutMultipartStream), "PUT", "__stream/{id}")]
        [InlineData(nameof(GenericController<MyClass, MyClass>.GetAll), "GET", null)]
        [InlineData(nameof(GenericController<MyClass, MyClass>.GetById), "GET", "{id}")]
        [InlineData(nameof(GenericController<MyClass, MyClass>.Put), "PUT", "{id}")]
        public void ValidateVerbs(string methodName, string expHttpVerb, string expTemplate)
        {
            var type = typeof(GenericController<,>);
            var mi = type.GetMethod(methodName);
            var att = mi.GetCustomAttributes(typeof(HttpMethodAttribute)).First() as HttpMethodAttribute;
            att.HttpMethods.First().ShouldBe(expHttpVerb);
            att.Template.ShouldBe(expTemplate);
        }
        public class MyClass : IDomainEntity
        {
            public string Id { get; set; }
        }
    }
}