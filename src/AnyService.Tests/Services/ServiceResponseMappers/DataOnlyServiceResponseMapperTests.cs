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
    public class DataOnlyServiceResponseMapperTests
    {
        [Fact]
        public void ToActionResult_ValidateConvertableItemCount()
        {
            var allSrvResults = ServiceResult.All;
            DataOnlyServiceResponseMapper.ConversionFuncs.Keys.Count().ShouldBe(allSrvResults.Count());

            foreach (var sr in allSrvResults)
                DataOnlyServiceResponseMapper.ConversionFuncs.ContainsKey(sr);
        }

        [Fact]
        public void ToActionResult_InvalidCastThrows()
        {
            var serRes = new ServiceResponse
            {
                Result = ServiceResult.Ok,
                Data = new object(),
            };
            var mapper = new DataOnlyServiceResponseMapper();
            Should.Throw(() => mapper.Map<TestClass1, object>(serRes), typeof(InvalidOperationException));
        }

        [Theory]
        [MemberData(nameof(ReturnExpectedActionResultMember_DATA))]
        public void ReturnExpectedActionResult(string result, TestClass1 data, string message, Type expectedActionResultType)
        {
            var serRes = new ServiceResponse
            {
                Result = result,
                Data = data,
                Message = message
            };
            var mapper = new DataOnlyServiceResponseMapper();
            mapper.Map<TestClass1, TestClass2>(serRes).ShouldBeOfType(expectedActionResultType);

            if (result == ServiceResult.Ok && data != null)
                (serRes.Data as TestClass2).Id.ShouldBe(data.Id.ToString());
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