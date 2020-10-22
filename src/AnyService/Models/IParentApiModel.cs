using System.Collections.Generic;

namespace AnyService.Models
{
    public interface IParentApiModel<TEntity> where TEntity : IEntity
    {
        public string ChildEntityKey { get; }
        public IEnumerable<TEntity> Childs { get; set; }
    }
}
