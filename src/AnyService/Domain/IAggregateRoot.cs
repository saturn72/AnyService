using System.Collections.Generic;

namespace AnyService.Domain
{
    public interface IAggregateRoot<TChildEntity> where TChildEntity : IDomainEntity
    {
        IEnumerable<TChildEntity> Children { get; set; }
    }
}
