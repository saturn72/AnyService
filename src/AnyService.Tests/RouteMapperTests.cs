using System;
using System.Collections.Generic;
using System.Reflection;
using Shouldly;
using Xunit;

namespace AnyService.Tests
{
    public class Myclass
    {

    }
    public class RouteMapperTests
    {
        [Fact]
        public void RouteMapper_CreatedTests()
        {
            var type = typeof(AnyService.RouteMapper);
            var allCtors = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            var ctor = allCtors[0];
            var expType = typeof(Myclass);
            var expRoutePrefix = "some-route-prefix";
            var maps = new Dictionary<Type, TypeConfigRecord>
            {
                {expType, new TypeConfigRecord(expType, expRoutePrefix, null)}
            };
            var instance = ctor.Invoke(new object[] { maps });
            (instance as RouteMapper).Maps.Count.ShouldBe(1);
            (instance as RouteMapper).Maps[expRoutePrefix].ShouldBe(expType);
        }
    }
}