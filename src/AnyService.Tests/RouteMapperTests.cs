using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace AnyService.Tests
{
    public class MyClass
    {

    }
    public class RouteMapperTests
    {
        [Fact]
        public void RouteMapper_CreatedTests()
        {
            var type = typeof(EntityConfigRecordManager);
            var pi = type.GetProperty(nameof(EntityConfigRecordManager.EntityConfigRecords));

            var expType = typeof(MyClass);
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