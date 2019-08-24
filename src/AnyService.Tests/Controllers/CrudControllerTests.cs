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
            route.Template.ShouldBe("_anyservice/{entityName}");
        }
        [Theory]
        [InlineData("Post", "POST", "")]
        [InlineData("GetAll", "GET", "")]
        [InlineData("Get", "GET", "{id}")]
        [InlineData("Put", "PUT", "{id}")]
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