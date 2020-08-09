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

            var expType = typeof(MyClass);
            var expRoutePrefix = "/some-route-prefix";
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

            var ecrm = new EntityConfigRecordManager
            {
                EntityConfigRecords = maps
            };

            ecrm.EntityConfigRecords.Count().ShouldBe(1);
            ecrm.EntityConfigRecords.First(c => c.Route.Equals(expRoutePrefix, StringComparison.InvariantCultureIgnoreCase)).Type.ShouldBe(expType);
            ecrm.GetRecord(expType).Route.Value.ShouldBe(expRoutePrefix);
        }
    }
}