using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyService.Controllers;
using AnyService.Services;
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
        [InlineData(nameof(GenericController<MyClass, MyClass>.UpdateEntityMappings), "PUT", "__map/{id}")]
        public void ValidateVerbs(string methodName, string expHttpVerb, string expTemplate)
        {
            var type = typeof(GenericController<,>);
            var mi = type.GetMethod(methodName);
            var att = mi.GetCustomAttributes(typeof(HttpMethodAttribute)).First() as HttpMethodAttribute;
            att.HttpMethods.First().ShouldBe(expHttpVerb);
            att.Template.ShouldBe(expTemplate);
        }
        public class MyClass : IDomainEntity
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
        #region UpdateEntityMappings
        [Fact]
        public async Task UpdateEntityMappings_InvalidModelState()
        {
            var wc = new WorkContext
            {
                CurrentEndpointSettings = new EndpointSettings
                {
                    MapToType = typeof(MyClass),
                    MapToPaginationType = typeof(Pagination<MyClass>),
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(MyClass),
                        Name = typeof(MyClass).Name,
                    }
                }
            };
            var log = new Mock<ILogger<GenericController<MyClass, MyClass>>>();
            var ctrl = new GenericController<MyClass, MyClass>(null, null, null, wc, log.Object);
            ctrl.ModelState.AddModelError("k", "err");
            var res = await ctrl.UpdateEntityMappings("id", null);
            res.ShouldBeOfType<BadRequestResult>();
        }
        #endregion
    }
}