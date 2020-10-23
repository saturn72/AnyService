using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyService.Controllers;
using AnyService.Models;
using AnyService.Services;
using AnyService.Services.EntityMapping;
using AnyService.Services.ServiceResponseMappers;
using AnyService.Tests.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Controllers
{
    public class GenericControllerTests : MappingTest
    {
        [Theory]
        [InlineData(nameof(GenericController<MyClass, MyClass>.Post), "POST", null)]
        [InlineData(nameof(GenericController<MyClass, MyClass>.PostMultipart), "POST", "__multipart")]
        [InlineData(nameof(GenericController<MyClass, MyClass>.PostMultipartStream), "POST", "__stream")]
        [InlineData(nameof(GenericController<MyClass, MyClass>.PutMultipartStream), "PUT", "__stream/{id}")]
        [InlineData(nameof(GenericController<MyClass, MyClass>.GetAll), "GET", null)]
        [InlineData(nameof(GenericController<MyClass, MyClass>.GetById), "GET", "{id}")]
        [InlineData(nameof(GenericController<MyClass, MyClass>.Put), "PUT", "{id}")]

        [InlineData(nameof(EntityMappingRecordController.UpdateEntityMappings), "PUT", "__map/{id}")]
        public void ValidateVerbs(string methodName, string expHttpVerb, string expTemplate)
        {
            var type = typeof(GenericController<,>);
            var mi = type.GetMethod(methodName);
            var att = mi.GetCustomAttributes(typeof(HttpMethodAttribute)).First() as HttpMethodAttribute;
            att.HttpMethods.First().ShouldBe(expHttpVerb);
            att.Template.ShouldBe(expTemplate);
        }
        public class MyClass : IEntity
        {
            public string Id { get; set; }
        }
        #region Post
        [Fact]
        public async Task CreateReturnsBadRequestOnInvalidModel()
        {
            var t = typeof(Category);
            var wc = new WorkContext
            {
                CurrentEndpointSettings = new EndpointSettings
                {
                    MapToType = typeof(CategoryModel),
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Name = t.Name,
                        Type = t,
                    }
                }
            };
            var log = new Mock<ILogger<GenericController<CategoryModel, Category>>>();
            var ctrl = new GenericController<CategoryModel, Category>(
                null, null,
                null,
                wc, log.Object);

            ctrl.ModelState.AddModelError("k", "err");
            var res = await ctrl.Post(null);
            res.ShouldBeOfType<BadRequestObjectResult>();
        }
        [Fact]
        public async Task Create_ReturnsServiceResponse()
        {
            var log = new Mock<ILogger<GenericController<Category, Category>>>();
            var t = typeof(Category);
            var wc = new WorkContext
            {
                CurrentEndpointSettings = new EndpointSettings
                {
                    MapToType = t,
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Name = t.Name,
                        Type = t,
                    }
                }
            };
            var srvRes = new ServiceResponse<Category>
            {
                Result = ServiceResult.Accepted,
                Message = "this is message"
            };
            var srv = new Mock<ICrudService<Category>>();
            srv.Setup(s => s.Create(It.IsAny<Category>())).ReturnsAsync(srvRes);

            var sm = new Mock<IServiceResponseMapper>();
            var expData = "data";
            sm.Setup(s => s.MapServiceResponse(typeof(Category), typeof(Category), It.IsAny<ServiceResponse>()))
                .Returns(new JsonResult(expData));
            var ctrl = new GenericController<Category, Category>(
                srv.Object, null,
                sm.Object, wc,
                log.Object);

            var model = new Category();
            var res = await ctrl.Post(model);
            var js = res.ShouldBeOfType<JsonResult>();
            js.Value.ShouldBe(expData);
        }
        #endregion
    }
    public class EntityMappingControllerTests
    {
        public class MyClass : IEntity
        {
            public string Id { get; set; }
        }
        #region UpdateEntityMappings
        [Fact]
        public async Task UpdateEntityMappings_InvalidModelState_ReturnsBadRequest()
        {
            var ecrs = new EntityConfigRecord[] { };
            var log = new Mock<ILogger<EntityMappingRecordController>>();
            var ctrl = new EntityMappingRecordController(null, ecrs, null, log.Object);
            ctrl.ModelState.AddModelError("k", "err");
            var m = new EntityMappingRequestModel { ParentEntityKey = "exists", ChildEntityKey = "exists" };
            var res = await ctrl.UpdateEntityMappings("id", m);
            res.ShouldBeOfType<BadRequestResult>();
        }
        [Theory]
        [MemberData(nameof(UpdateEntityMappings_InvalidRequest_ReturnsBadRequest_DATA))]
        public async Task UpdateEntityMappings_InvalidRequest_ReturnsBadRequest(EntityMappingRequestModel m)
        {
            var ecrs = new[]
            {
                new EntityConfigRecord{Name = "exists", ExternalName="exist"}
            };
            var log = new Mock<ILogger<EntityMappingRecordController>>();
            var ctrl = new EntityMappingRecordController(null, ecrs, null, log.Object);
            var res = await ctrl.UpdateEntityMappings("id", m);
            res.ShouldBeOfType<BadRequestResult>();
        }
        public static IEnumerable<object[]> UpdateEntityMappings_InvalidRequest_ReturnsBadRequest_DATA => new[]
        {
            new object[]{new EntityMappingRequestModel { ParentEntityKey = "not-exists", ChildEntityKey = "Exists" } },
            new object[]{new EntityMappingRequestModel { ParentEntityKey = "exists", ChildEntityKey = "not-exists"} },
        };
        [Fact]
        public async Task UpdateEntityMappings_ReturnsserviceResponse()
        {
            var ecrs = new[]
            {
                new EntityConfigRecord{Name = "e1", ExternalName="e1"},
                new EntityConfigRecord{Name = "e2", ExternalName="e2"},
            };
            var mgr = new Mock<IEntityMappingRecordManager>();
            mgr.Setup(m => m.UpdateMapping(It.IsAny<EntityMappingRequest>())).ReturnsAsync(new ServiceResponse<EntityMappingRequest> { Result = ServiceResult.Accepted });

            var srm = new Mock<IServiceResponseMapper>();

            var log = new Mock<ILogger<EntityMappingRecordController>>();
            var ctrl = new EntityMappingRecordController(mgr.Object, ecrs, srm.Object, log.Object);

            var model = new EntityMappingRequestModel { ParentEntityKey = "e1", ChildEntityKey = "e2" };
            await ctrl.UpdateEntityMappings("id", model);

            srm.Verify(s => s.MapServiceResponse(It.Is<ServiceResponse<EntityMappingRequest>>(sr => sr.Result == ServiceResult.Accepted)), Times.Once);
        }
        #endregion

    }
}