using AnyService.Events;
using AnyService.Infrastructure;
using AnyService.Services;
using AnyService.Utilities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace AnyService.Tests.Services.ServiceResponseMappers
{
    public class ServiceResponseWrapperExtensionsTests : MappingTest
    {
        #region Validate & Publish Exceptrion
        [Fact]
        public void ValidateServiceResponseAndPublishException_PublishException()
        {
            var eventPublished = false;
            string exId = "exId",
                eventKey = "ek";
            var ig = new Mock<IIdGenerator>();
            ig.Setup(i => i.GetNext()).Returns(exId);

            var eb = new Mock<IEventBus>();
            eb.Setup(e => e.Publish(It.Is<string>(s => s == eventKey), It.Is<DomainEventData>(d => d.Data.GetPropertyValueByName<string>("exceptionId") == exId)))
                .Callback(() => eventPublished = true);

            var wc = new WorkContext
            {
                CurrentEntityConfigRecord = new EntityConfigRecord { EventKeys = new EventKeyRecord("create", null, null, null) },
            };

            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IIdGenerator))).Returns(ig.Object);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            var eCtx = new Mock<IAppEngine>();
            eCtx.Setup(e => e.GetService<IIdGenerator>()).Returns(ig.Object);
            eCtx.Setup(e => e.GetService<IEventBus>()).Returns(eb.Object);
            eCtx.Setup(e => e.GetService<WorkContext>()).Returns(wc);

            AnyServiceAppContext.Init(eCtx.Object);

            var serviceResponse = new ServiceResponse();
            var w = new ServiceResponseWrapper(serviceResponse);
            var pi = typeof(ServiceResponseWrapper).GetProperty("Exception");
            pi.SetValue(w, new Exception());

            ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<int>(w, eventKey, "ddd");
            eventPublished.ShouldBeTrue();
            serviceResponse.ExceptionId.ShouldBe(exId);
        }
        [Fact]
        public void ValidateServiceResponseAndPublishException_ReturnServiceResponse_False()
        {
            var allFaultedResults = ServiceResult.All.Where(r => r != ServiceResult.Ok && r != ServiceResult.Accepted);

            foreach (var fr in allFaultedResults)
            {
                var srvRes = new ServiceResponse()
                {
                    Result = fr
                };
                var w = new ServiceResponseWrapper(srvRes);
                ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<int>(w, "ek", "ddd").ShouldBeFalse();
            }
        }
        [Fact]
        public void ValidateServiceResponseAndPublishException_ReturnServiceResponse_False_InvalidDataObject()
        {
            var srvRes = new ServiceResponse()
            {
                Data = "name",
                Result = ServiceResult.Ok
            };
            var w = new ServiceResponseWrapper(srvRes);
            ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<int>(w, "ek", "ddd").ShouldBeFalse();
        }
        [Fact]
        public void ValidateServiceResponseAndPublishException_ReturnServiceResponse_True_Accepted()
        {
            var srvRes = new ServiceResponse()
            {
                Data = "name",
                Result = ServiceResult.Accepted
            };
            var w = new ServiceResponseWrapper(srvRes);
            ServiceResponseWrapperExtensions.ValidateServiceResponseAndPublishException<int>(w, "ek", "ddd").ShouldBeTrue();
        }
        [Fact]
        public void ValidateServiceResponseAndPublishException_ReturnServiceResponse_True_Ok()
        {
            var srvRes = new ServiceResponse()
            {
                Data = "name",
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
            var serviceResponse = new ServiceResponse();
            var w = new ServiceResponseWrapper(serviceResponse);
            ServiceResponseWrapperExtensions.PublishExceptionIfExists(w, "ek", "ddd").ShouldBeFalse();
        }
        #endregion
    }
}
