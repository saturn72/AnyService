using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services.ServiceResponseMappers
{
    public class DataOnlyServiceResponseMapperTests : MappingTest
    {
        public DataOnlyServiceResponseMapperTests()
        {
            MappingExtensions.Configure(null, c =>
            {
                c.CreateMap<TestClass1, TestClass2>();
            });
        }
        [Fact]
        public void ToActionResult_ValidateConvertableItemCount()
        {
            var allSrvResults = ServiceResult.All;
            DataOnlyServiceResponseMapper.ConversionFuncs.Keys.Count().ShouldBe(allSrvResults.Count());

            foreach (var sr in allSrvResults)
                DataOnlyServiceResponseMapper.ConversionFuncs.ContainsKey(sr);
        }

        //[Fact]
        //public void ToActionResult_InvalidCastThrows()
        //{
        //    var serRes = new ServiceResponse<TestClass1>
        //    {
        //        Result = ServiceResult.Ok,
        //        Payload= new TestClass1(),
        //    };
        //    var mapper = new DataOnlyServiceResponseMapper();
        //    Should.Throw(() => mapper.MapServiceResponse<object, TestClass1>(serRes), typeof(InvalidOperationException));
        //}

        [Theory]
        [MemberData(nameof(ReturnExpectedActionResultMember_DATA))]
        public void ReturnExpectedActionResult(string result, TestClass1 payload, string message, Type expectedActionResultType)
        {
            var serRes = new ServiceResponse<TestClass1>
            {
                Result = result,
                Payload= payload,
                Message = message
            };
            var mapper = new DataOnlyServiceResponseMapper();
            var r = mapper.MapServiceResponse<TestClass1, TestClass2>(serRes);
            r.ShouldBeOfType(expectedActionResultType);

            if (result == ServiceResult.Ok && payload != null)
            {
                var ok = r.ShouldBeOfType<OkObjectResult>();
                (ok.Value as TestClass2).Id.ShouldBe(payload.Id.ToString());
            }
        }

        public static IEnumerable<object[]> ReturnExpectedActionResultMember_DATA =>
        new[]
        {
            new object[]{ ServiceResult.Accepted, new TestClass1(), null, typeof(AcceptedResult)},
            new object[]{ ServiceResult.Accepted, null, "some-message", typeof(AcceptedResult)},
            new object[]{ ServiceResult.Accepted, null, null, typeof(AcceptedResult)},

            new object[]{ ServiceResult.BadOrMissingData, new TestClass1(), null, typeof(BadRequestObjectResult)},
            new object[]{ ServiceResult.BadOrMissingData, null, "some-message", typeof(BadRequestObjectResult)},
            new object[]{ ServiceResult.BadOrMissingData, null, null, typeof(BadRequestResult)},

            new object[]{ ServiceResult.NotFound, new TestClass1(), null, typeof(NotFoundObjectResult)},
            new object[]{ ServiceResult.NotFound, null, "some-message", typeof(NotFoundObjectResult)},
            new object[]{ ServiceResult.NotFound, null, null, typeof(NotFoundResult)},

            new object[]{ ServiceResult.NotSet, new TestClass1(), null, typeof(StatusCodeResult) },
            new object[]{ ServiceResult.NotSet, null, "some-message", typeof(StatusCodeResult) },
            new object[]{ ServiceResult.NotSet, null, null, typeof(StatusCodeResult)},

            new object[]{ ServiceResult.Error, new TestClass1(), null, typeof(ObjectResult)},
            new object[]{ ServiceResult.Error, null, "some-message", typeof(ObjectResult)},
            new object[]{ ServiceResult.Error, null, null, typeof(StatusCodeResult)},

            new object[]{ ServiceResult.Ok, new TestClass1(), null, typeof(OkObjectResult)},
            new object[]{ ServiceResult.Ok, null, "some-message", typeof(OkObjectResult)},
            new object[]{ ServiceResult.Ok, null, null, typeof(OkResult)},

            new object[]{ ServiceResult.Unauthorized, new TestClass1(), null, typeof(UnauthorizedObjectResult)},
            new object[]{ ServiceResult.Unauthorized, null, "some-message", typeof(UnauthorizedObjectResult)},
            new object[]{ ServiceResult.Unauthorized, null, null, typeof(UnauthorizedResult)},
        };
    }
}