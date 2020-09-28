using AnyService.SampleApp.Controllers;
using AnyService.SampleApp.Domain;
using AnyService.SampleApp.Models;
using AnyService.SampleApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.SampleApp.ServicesConfigurars
{
    public class AnyServiceServicesConfigurar
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
                        ControllerSettings = new ControllerSettings
                        {
                            MapToType = typeof(CategoryModel)
                        }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(Product),
                        ControllerSettings = new ControllerSettings
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
                        ControllerSettings = new ControllerSettings
                        {Authorization = new AuthorizationInfo
                        {
                            ControllerAuthorizationNode = new AuthorizationNode{Roles = new[]{"some-role"}}
                        } }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(Dependent2),
                        ControllerSettings = new ControllerSettings
                        {
                            Route = "/api/d/",
                        },
                        CrudValidatorType = typeof(Dependent2AlwaysTrueCrudValidator)
                    },

                    new EntityConfigRecord
                    {
                        Type = typeof(MultipartSampleModel),
                    },
                    new EntityConfigRecord
                    {
                        ControllerSettings = new ControllerSettings
                        {
                            Route = "/v1/my-great-route",
                            ControllerType = typeof(CustomController),
                        },
                        Type = typeof(CustomModel),
                    },
                      new EntityConfigRecord
                    {
                        Type = typeof(CustomModel),
                        Name = "area2_cutomModel"
                    },
                }
            };

            services.AddAnyService(anyServiceConfig);
            return anyServiceConfig;
        }
    }
}
