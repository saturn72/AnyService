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
            ConstructAggregationData(type);
        }

        private void ConstructAggregationData(Type type)
        {
            Aggregations = ExtractAggregationData(type);
            IsAggregatedRoot = Aggregations.Any();
        }

        private IEnumerable<AggregationDataFactory> ExtractAggregationData(Type type)
        {
            var aggData = new List<AggregationDataFactory>();
            //foreach (var pi in type.GetProperties())
            //{
            //    var aggAtt = pi.GetCustomAttributes<AggregatedAttribute>(true);

            //    foreach (var aa in aggAtt)
            //        aggData.Add(new AggregationData
            //        {
            //            EntityName = aa.EntityName,
            //            IsEnumerable = pi.PropertyType.IsOfType<IEnumerable>()
            //        }); ;
            //}
            return aggData;
        }

        internal Type Type { get; }
        internal bool IsSoftDeleted { get; }
        internal bool ShowSoftDeleted { get; }
        internal bool IsCreatableAudit { get; }
        internal bool IsReadableAudit { get; }
        internal bool IsUpdatableAudit { get; }
        internal bool IsDeletableAudit { get; }
        internal bool IsAggregatedRoot { get; private set; }
        internal IEnumerable<AggregationDataFactory> Aggregations { get; private set; }
    }
}
