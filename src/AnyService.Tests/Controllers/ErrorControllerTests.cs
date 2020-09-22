using System.Linq;
using System.Reflection;
using AnyService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Controllers
{
    public class ErrorControllerTests 
    {
        [Fact]
        public void ValidateRoute()
        {
            var type = typeof(ErrorController);
            var route = type.GetCustomAttributes(typeof(RouteAttribute)).First() as RouteAttribute;
            route.Template.ShouldBe("__error");
        }
    }
}