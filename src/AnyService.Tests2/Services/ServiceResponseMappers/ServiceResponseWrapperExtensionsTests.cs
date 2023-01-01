using AnyService.Events;
using AnyService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnyService.Tests.Services.ServiceResponseMappers
{
    public class ServiceResponseWrapperExtensionsTests : MappingTest
    {
        #region Validate & Publish Exceptrion
        [Fact]
        public void ValidateServiceResponseAndPublishException_PublishException()
        {
            var eventPublished = false;
            string traceId = "exId",
                eventKey = "ek";
            var wc = new WorkContext
            {
                CurrentEntityConfigRecord = new EntityConfigRecord { EventKeys = new EventKeyRecord("create", null, null, null) },
                TraceId = traceId
            };
            var eb = new Mock<IDomainEventBus>();
            eb.Setup(e => e.Publish(It.Is<string>(s => s == eventKey), It.Is<DomainEvent>(d => d.Data.GetPropertyValueByName<string>("TraceId") == traceId)))
                .Callback(() => eventPublished = true);

            ServiceProviderMock.Setup(s => s.GetService(typeof(IDomainEventBus))).Returns(eb.Object);
            ServiceProviderMock.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            ServiceResponseWrapperExtensions.Init(ServiceProviderMock.Object);

            var serviceResponse = new ServiceResponse<object>();
            var w = new ServiceResponseWrapper(serviceResponse);
            var pi = typeof(ServiceResponseWrapper).GetProperty("Exception");
            pi.SetValue(w, new Exception());

            ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<object>(w, eventKey, "ddd");
            eventPublished.ShouldBeTrue();
            serviceResponse.TraceId.ShouldBe(traceId);
        }
        [Fact]
        public void ValidateServiceResponseAndPublishException_ReturnServiceResponse_False()
        {
            var allFaultedResults = ServiceResult.All.Where(r => r != ServiceResult.Ok && r != ServiceResult.Accepted);

            foreach (var fr in allFaultedResults)
            {
                var srvRes = new ServiceResponse<object>()
                {
                    Result = fr
                };
                var w = new ServiceResponseWrapper(srvRes);
                ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<object>(w, "ek", "ddd").ShouldBeFalse();
            }
        }
        [Fact]
        public void ValidateServiceResponseAndPublishException_ReturnServiceResponse_False_InvalidDataObject()
        {
            var srvRes = new ServiceResponse<string>()
            {
                Payload = "name",
                Result = ServiceResult.Ok
            };
            var w = new ServiceResponseWrapper(srvRes);
            ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<int>(w, "ek", "ddd").ShouldBeFalse();
        }
        [Fact]
        public void ValidateServiceResponseAndPublishException_ReturnServiceResponse_True_Accepted()
        {
            var srvRes = new ServiceResponse<string>()
            {
                Payload = "name",
                Result = ServiceResult.Accepted
            };
            var w = new ServiceResponseWrapper(srvRes);
            ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<int>(w, "ek", "ddd").ShouldBeTrue();
        }
        [Fact]
        public void ValidateServiceResponseAndPublishException_ReturnServiceResponse_True_Ok()
        {
            var srvRes = new ServiceResponse<string>()
            {
                Payload = "name",
                Result = ServiceResult.Ok
            };
            var w = new ServiceResponseWrapper(srvRes);
            ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<string>(w, "ek", "ddd").ShouldBeTrue();
        }


        #endregion
        #region PublishExceptionIfExists
        [Fact]
        public void PublishExceptionIfExists_DoesNotPublish()
        {
            var serviceResponse = new ServiceResponse<object>();
            var w = new ServiceResponseWrapper(serviceResponse);
            ServiceResponseWrapperExtensions.PublishExceptionIfExists(w, "ek", "ddd").ShouldBeFalse();
        }
        #endregion
    }
}
