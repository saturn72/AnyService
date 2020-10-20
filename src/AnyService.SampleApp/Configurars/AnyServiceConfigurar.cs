using AnyService.SampleApp.Controllers;
using AnyService.SampleApp.Entities;
using AnyService.SampleApp.Models;
using AnyService.SampleApp.Services;
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
                        EndpointSettings = new []
                        {
                            new EndpointSettings
                            {
                                Area = "admin"
                            },
                            new EndpointSettings
                            {
                        ShowSoftDeleted = false,
                                MapToType = typeof(CategoryModel)
                            },
                        }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(Product),
                        EndpointSettings = new []
                        {
                            new EndpointSettings
                            {
                                MapToType = typeof(ProductModel)
                            }
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
                        EndpointSettings =  new []
                        {
                            new EndpointSettings
                            {
                                Authorization = new AuthorizeAttribute{Roles = "some-role"}
                            }
                        }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(Dependent2),
                        EndpointSettings = new[]
                        {
                            new EndpointSettings
                            {
                             Route = "/api/d/",
                            }
                        },
                        CrudValidatorType = typeof(Dependent2AlwaysTrueCrudValidator)
                    },

                    new EntityConfigRecord
                    {
                        Type = typeof(MultipartSampleModel),
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(CustomEntity),
                        EndpointSettings = new []
                        {
                            new EndpointSettings
                            {
                                Name = "area2_cutomModel",
                            },
                            new EndpointSettings
                            {
                                Route = "/v1/my-great-route",
                                ControllerType = typeof(CustomController),
                            },
                            new EndpointSettings
                            {
                                Name = "method_not_allowed",
                                Route = "/api/na",
                                PostSettings = new EndpointMethodSettings{Active = false },
                                PutSettings = new EndpointMethodSettings{Active = false },
                                DeleteSettings = new EndpointMethodSettings{Active = false },
                            },
                        }
                    },
                }
            };

            services.AddAnyService(anyServiceConfig);
            return anyServiceConfig;
        }
    }
}
