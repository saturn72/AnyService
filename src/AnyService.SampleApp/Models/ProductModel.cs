using AnyService.Models;
using System.Collections.Generic;

namespace AnyService.SampleApp.Models
{
    public class ProductModel : IParentApiModel<ProductAttribute>
    {
        public string ChildEntityKey => throw new System.NotImplementedException();

        public IEnumerable<ProductAttribute> Childs { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}
