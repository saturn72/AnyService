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
            var type = typeof(EntityConfigRecordManager);
            var pi = type.GetProperty(nameof(EntityConfigRecordManager.EntityConfigRecords));

            var expType = typeof(Myclass);
            var expRoutePrefix = "some-route-prefix";
            var maps = new[]
            {
              new EntityConfigRecord
              {
                  Type= expType,
                  Route =  expRoutePrefix,
                  EventKeys = null,
                  PermissionRecord =null,
                  EntityKey =  null,
                }
            };

            pi.SetValue(null, maps);
            EntityConfigRecordManager.EntityConfigRecords.Count().ShouldBe(1);
            EntityConfigRecordManager.EntityConfigRecords.First(c => c.Route.Equals(expRoutePrefix, StringComparison.InvariantCultureIgnoreCase)).Type.ShouldBe(expType);
            EntityConfigRecordManager.GetRecord(expType).Route.ShouldBe(expRoutePrefix);
        }
    }
}