using AnyService.Audity;
using System;

namespace AnyService.Services.Internals
{
    internal class DomainEntityMetadata
    {
        internal DomainEntityMetadata(Type type)
        {
            Type = type;
            var isSoftDeleted = type.IsOfType<ISoftDelete>();
            IsSoftDeleted = isSoftDeleted;;
            IsCreatableAudit = type.IsOfType<ICreatableAudit>();
            IsReadableAudit = type.IsOfType<IReadableAudit>();
            IsUpdatableAudit = type.IsOfType<IUpdatableAudit>();
            IsDeletableAudit = type.IsOfType<IDeletableAudit>();
        }
        internal Type Type { get; }
        internal bool IsSoftDeleted { get; }
        internal bool IsCreatableAudit { get; }
        internal bool IsReadableAudit { get; }
        internal bool IsUpdatableAudit { get; }
        internal bool IsDeletableAudit { get; }
    }
}
