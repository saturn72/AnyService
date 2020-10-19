using AnyService.Audity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnyService.Services.Internals
{
    internal class DomainEntityMetadata
    {
        internal DomainEntityMetadata(Type type, bool showSoftDeleted)
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
        internal Type Type { get; }
        internal bool IsSoftDeleted { get; }
        internal bool ShowSoftDeleted { get; }
        internal bool IsCreatableAudit { get; }
        internal bool IsReadableAudit { get; }
        internal bool IsUpdatableAudit { get; }
        internal bool IsDeletableAudit { get; }
    }
}
