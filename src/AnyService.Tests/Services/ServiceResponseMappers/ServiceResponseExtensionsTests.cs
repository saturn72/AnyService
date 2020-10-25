using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AnyService.Tests.Services.ServiceResponseMappers
{
    public class ServiceResponseExtensionsTests : MappingTest
    {

        [Fact]
        public void ToActionResult_DelegatesCallToOverload()
        {
            var serRes = new ServiceResponse<object>
            {
                Result = ServiceResult.Ok,
                Payload = new object(),
            };
            var srm = new Mock<IServiceResponseMapper>();
            ServiceResponseMapperExtensions.MapServiceResponse<object>(srm.Object, serRes);
            srm.Verify(s => s.MapServiceResponse(
                It.Is<Type>(t => t == typeof(object)),
                It.Is<ServiceResponse>(sr => serRes == sr)),
                Times.Once);
        }

        [Fact]
        public void ToActionResult_ValidateConvertableItemCount()
        {
            var allSrvResults = ServiceResult.All;
            ServiceResponseExtensions.ConversionFuncs.Keys.Count().ShouldBe(allSrvResults.Count());

            foreach (var sr in allSrvResults)
                ServiceResponseExtensions.ConversionFuncs.ContainsKey(sr);
        }

        [Fact]
        public void ToActionResult_InvalidCastThrows()
        {
            var serRes = new ServiceResponse<object>
            {
                Result = ServiceResult.Ok,
                Payload = new object(),
            };
            Should.Throw(() => ServiceResponseExtensions.ToActionResult(serRes, typeof(object), "default"), typeof(InvalidOperationException));
        }

        [Theory]
        [MemberData(nameof(ReturnExpectedActionResultMember_DATA))]
        public void ReturnExpectedActionResult(string result, TestClass1 payload, string message, Type expectedActionResultType)
        {
            var serRes = new ServiceResponse<TestClass1>
            {
                Result = result,
                Payload = payload,
                Message = message
            };
            var r = ServiceResponseExtensions.ToActionResult(serRes, typeof(TestClass2), "default");
            r.ShouldBeOfType(expectedActionResultType);
        }
        [Fact]
        public void Maps_OkObjectResult()
        {
            string id = "1", msg = "msg";
            var serRes = new ServiceResponse<TestClass1>
            {
                Result = ServiceResult.Ok,
                Payload = new TestClass1 { Id = int.Parse(id) },
                Message = msg
            };
            var r = ServiceResponseExtensions.ToActionResult(serRes, typeof(TestClass2), "default");
            var ok = r.ShouldBeOfType<OkObjectResult>();

            ok.Value.GetPropertyValueByName<string>("message").ShouldBe(msg);
            var tc = ok.Value.GetPropertyValueByName<TestClass2>("data");
            tc.Id.ShouldBe(id);
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

        #region ToHttpStatusCode
        [Theory]
        [InlineData(ServiceResult.Accepted, StatusCodes.Status202Accepted)]
        [InlineData(ServiceResult.BadOrMissingData, StatusCodes.Status400BadRequest)]
        [InlineData(ServiceResult.NotFound, StatusCodes.Status404NotFound)]
        [InlineData(ServiceResult.NotSet, StatusCodes.Status403Forbidden)]
        [InlineData(ServiceResult.Ok, StatusCodes.Status200OK)]
        [InlineData(ServiceResult.Unauthorized, StatusCodes.Status401Unauthorized)]
        [InlineData("not-exists", StatusCodes.Status500InternalServerError)]
        public void ToHttpStatusCode_AllCodes(string result, int exp)
        {
            new ServiceResponse { Result = result }.ToHttpStatusCode().ShouldBe(exp);
        }
        #endregion
        #region ValidateServiceResponse
        [Theory]
        [MemberData(nameof(ValidateServiceResponse_ReturnsFalse_DATA))]
        public void ValidateServiceResponse_ReturnsFalse(ServiceResponse serviceResponse)
        {
            serviceResponse.ValidateServiceResponse<int>().ShouldBeFalse();
        }
        public static IEnumerable<object[]> ValidateServiceResponse_ReturnsFalse_DATA => new[]
        {
            new object[] { null},
            new object[] { new ServiceResponse{Result = ServiceResult.Error}},
            new object[] { new ServiceResponse<string>{Result = ServiceResult.Ok, Payload = "this is data"}},
        };
        [Theory]
        [MemberData(nameof(ValidateServiceResponse_ReturnsTrue_DATA))]
        public void ValidateServiceResponse_ReturnsTrue(ServiceResponse serviceResponse)
        {
            serviceResponse.ValidateServiceResponse<string>().ShouldBeTrue();
        }
        public static IEnumerable<object[]> ValidateServiceResponse_ReturnsTrue_DATA => new[]
        {
            new object[] { new ServiceResponse<string>{Result = ServiceResult.Ok, Payload = "this is data"}},
            new object[] { new ServiceResponse{Result = ServiceResult.Accepted}},
        };
    }
    #endregion
}