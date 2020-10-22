using System.Collections.Generic;

namespace AnyService.Tests.Services.ServiceResponseMappers
{
    public class TestClass1
    {
        public int Id { get; set; }
    }
    public class TestClass2
    {
        public string Id { get; set; }
    }
    public class Product : IEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class Category : IEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class ProductModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class CategoryModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<ProductModel> Products { get; set; }
    }

    public abstract class MappingTest
    {
        static MappingTest()
        {
            MappingExtensions.Configure(cfg =>
            {
                cfg.CreateMap<TestClass1, TestClass2>()
                    .ForMember(dest => dest.Id, opts => opts.MapFrom(src => src.Id.ToString()));

                cfg.CreateMap<Product, ProductModel>();
                cfg.CreateMap<ProductModel, Product>();
                cfg.CreateMap<Category, CategoryModel>();
                cfg.CreateMap<CategoryModel, Category>();

            });
        }
    }
}