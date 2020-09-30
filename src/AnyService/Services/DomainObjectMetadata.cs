using AnyService.Audity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public class DomainObjectMetadataFactory : IFactory<Type, DomainObjectMetadata>
    {
        public DomainObjectMetadataFactory(IEnumerable<DomainObjectMetadata> enteries)
        {
            Enteries = enteries.ToDictionary(k => k.Type, v => v);
        }
        public virtual Task<DomainObjectMetadata> Get(Type key) => Task.FromResult(Enteries.GetValueOrDefault(key));
        internal IReadOnlyDictionary<Type, DomainObjectMetadata> Enteries { get; }
    }
    public class DomainObjectMetadata
    {
        public DomainObjectMetadata(Type type, bool showSoftDeleted)
        {
            Type = type;
            var isSoftDeleted = type.IsOfType<ISoftDelete>();
            IsSoftDeleted = isSoftDeleted;
            ShowSoftDeleted = isSoftDeleted && showSoftDeleted;
            IsCreatableAudit = type.IsOfType<ICreatableAudit>();
            IsReadableAudit = type.IsOfType<IReadableAudit>();
            IsUpdatableAudit = type.IsOfType<IUpdatableAudit>();
            IsDeletableAudit = type.IsOfType<IDeletableAudit>();
        }
        public Type Type { get; }
        public bool IsSoftDeleted { get; }
        public bool ShowSoftDeleted { get; }
        public bool IsCreatableAudit { get; }
        public bool IsReadableAudit { get; }
        public bool IsUpdatableAudit { get; }
        public bool IsDeletableAudit { get; }
    }
}
