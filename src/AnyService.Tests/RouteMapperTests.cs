using System;
using System.Collections.Generic;
using System.Linq;
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
            var type = typeof(TypeConfigRecordManager);
            var pi = type.GetProperty("TypeConfigRecords");

            var expType = typeof(Myclass);
            var expRoutePrefix = "some-route-prefix";
            var maps = new[]
            {
              new TypeConfigRecord(expType, expRoutePrefix, null, null, null),
            };

            pi.SetValue(null, maps);
            TypeConfigRecordManager.TypeConfigRecords.Count().ShouldBe(1);
            TypeConfigRecordManager.TypeConfigRecords.First(c => c.RoutePrefix.Equals(expRoutePrefix, StringComparison.InvariantCultureIgnoreCase)).Type.ShouldBe(expType);
            TypeConfigRecordManager.GetRecord(expType).RoutePrefix.ShouldBe(expRoutePrefix);
        }
    }
}