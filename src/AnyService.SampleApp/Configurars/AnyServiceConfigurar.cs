using AnyService.SampleApp.Controllers;
using AnyService.SampleApp.Entities;
using AnyService.SampleApp.Models;
using AnyService.SampleApp.Services;
using AnyService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.SampleApp.Configurars
{
    public class AnyServiceConfigurar
    {
        public AnyServiceConfig Configure(IServiceCollection services)
        {
            var anyServiceConfig = new AnyServiceConfig
            {
                EntityConfigRecords = new[]
                {
                    new EntityConfigRecord
                    {
                        Type = typeof(Category),
                        EndpointSettings = new EndpointSettings
                        {
                            Area = "admin"
                        }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(Category),
                        ShowSoftDelete = true,
                        EndpointSettings = new EndpointSettings
                        {
                            MapToType = typeof(CategoryModel)
                        }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(Product),
                        EndpointSettings = new EndpointSettings
                        {
                            MapToType = typeof(ProductModel)
                        }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(ProductAttribute)
                    },
                      new EntityConfigRecord
                    {
                        Type =   typeof(DependentModel),
                    },
                    new EntityConfigRecord
                    {
                        Type =   typeof(Stock),
                        EndpointSettings = new EndpointSettings
                        {
                            Authorization = new AuthorizeAttribute{Roles = "some-role"}
                        }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(Dependent2),
                        EndpointSettings = new EndpointSettings
                        {
                            Route = "/api/d/",
                        },
                        CrudValidatorType = typeof(Dependent2AlwaysTrueCrudValidator)
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(MyModel),
                        EndpointSettings = new EndpointSettings
                        {
                            Route = "/api/mymodel",
                        },
                        CrudValidatorType = typeof(AlwaysTrueCrudValidator<MyModel>)
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(MultipartSampleModel),
                    },
                    new EntityConfigRecord
                    {
                        EndpointSettings = new EndpointSettings
                        {
                            Route = "/v1/my-great-route",
                            ControllerType = typeof(CustomController),
                        },
                        Type = typeof(CustomEntity),
                    },
                      new EntityConfigRecord
                    {
                        Type = typeof(CustomEntity),
                        Name = "area2_cutomModel"
                    },
                      new EntityConfigRecord
                    {
                        Type = typeof(CustomEntity),
                        Name = "method_not_allowed",
                        EndpointSettings = new EndpointSettings
                        {
                            Route = "/api/na",
                            PostSettings = new EndpointMethodSettings{Disabled = true },
                            PutSettings = new EndpointMethodSettings{Disabled = true },
                            DeleteSettings = new EndpointMethodSettings{Disabled = true },
                        }
                    },
                }
            };

            services.AddAnyService(anyServiceConfig);
            return anyServiceConfig;
        }
    }
}
