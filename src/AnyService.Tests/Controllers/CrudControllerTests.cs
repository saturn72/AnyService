using System.Linq;
using System.Reflection;
using AnyService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Controllers
{
    public class CrudControllerTests
    {
        [Fact]
        public void ValidateRoute()
        {
            var type = typeof(CrudController);
            var route = type.GetCustomAttributes(typeof(RouteAttribute)).First() as RouteAttribute;
            route.Template.ShouldBe("__anyservice");
        }
        [Theory]
        [InlineData("Post", "POST", "{entityName}")]
        [InlineData("Post", "POST", "{entityName}")]
        [InlineData("PostMultipart", "POST", "__multipart/{entityName}")]
        [InlineData("PostMultipartStream", "POST", "__multipart/{entityName}/__stream")]
        [InlineData("GetAll", "GET", "{entityName}")]
        [InlineData("Get", "GET", "{entityName}/{id}")]
        [InlineData("Put", "PUT", "{entityName}/{id}")]
        public void ValidateVerbs(string methodName, string expHttpVerb, string expTemplate)
        {
            var type = typeof(CrudController);
            var mi = type.GetMethod(methodName);
            var att = mi.GetCustomAttributes(typeof(HttpMethodAttribute)).First() as HttpMethodAttribute;
            att.HttpMethods.First().ShouldBe(expHttpVerb);
            att.Template.ShouldBe(expTemplate);
        }
    }
}