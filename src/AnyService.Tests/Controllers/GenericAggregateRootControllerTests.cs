using AnyService.Controllers;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using AnyService.Tests.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Controllers
{
    public class GenericAggregateRootControllerTests : MappingTest
    {
        #region Create
        [Fact]
        public async Task Create_ReturnsBadRequestOnInvalidModel()
        {
            var log = new Mock<ILogger<GenericAggregateRootController<CategoryModel, Category>>>();
            var ctrl = new GenericAggregateRootController<CategoryModel, Category>(null, null, log.Object);

            ctrl.ModelState.AddModelError("k", "err");
            var res = await ctrl.Post(null);
            res.ShouldBeOfType<BadRequestObjectResult>();
        }
        [Fact]
        public async Task Create_ReturnsServiceResponse()
        {
            var log = new Mock<ILogger<GenericAggregateRootController<CategoryModel, Category>>>();

            var srvRes = new ServiceResponse<Category>
            {
                Result = ServiceResult.Accepted,
                Message = "this is message"
            };
            var srv = new Mock<ICrudService<Category>>();
            srv.Setup(s => s.Create(It.IsAny<Category>())).ReturnsAsync(srvRes);

            var sm = new Mock<IServiceResponseMapper>();
            var expData = "data";
            sm.Setup(s => s.MapServiceResponse(typeof(Category), typeof(CategoryModel), It.IsAny<ServiceResponse>()))
                .Returns(new JsonResult(expData));
            var ctrl = new GenericAggregateRootController<CategoryModel, Category>(srv.Object, sm.Object, log.Object);

            var model = new CategoryModel();
            var res = await ctrl.Post(model);
            var js = res.ShouldBeOfType<JsonResult>();
            js.Value.ShouldBe(expData);
        }
        #endregion
        #region Get By If
        public async Task GetById_NoAggregatedReturn()
        {
            var log = new Mock<ILogger<GenericAggregateRootController<CategoryModel, Category>>>();

            var srvRes = new ServiceResponse<Category>
            {
                Result = ServiceResult.Accepted,
                Message = "this is message"
            };
            var srv = new Mock<ICrudService<Category>>();
            srv.Setup(s => s.Create(It.IsAny<Category>())).ReturnsAsync(srvRes);

            var sm = new Mock<IServiceResponseMapper>();
            var expData = "data";
            sm.Setup(s => s.MapServiceResponse(typeof(Category), typeof(CategoryModel), It.IsAny<ServiceResponse>()))
                .Returns(new JsonResult(expData));
            var ctrl = new GenericAggregateRootController<CategoryModel, Category>(srv.Object, sm.Object, log.Object);
            var res = await ctrl.GetById("id", "products, images");

            throw new NotImplementedException();
        }
        //get byId with navigation properties
        //get byId **without** navigation properties
        //get multiples - using paination with navigation properties
        //get nested types pagination
        #endregion
    }
}