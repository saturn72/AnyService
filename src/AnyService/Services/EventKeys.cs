using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService.Services
{
    public sealed class EventKeys
    {
        private readonly IReadOnlyDictionary<Type, EventKeyRecord> _eventKeyRecords;
        public EventKeys(IEnumerable<TypeConfigRecord> typeConfigRecords)
        {
            _eventKeyRecords = typeConfigRecords.ToDictionary(k => k.Type, v => v.EventKeyRecord);
        }
        public EventKeyRecord this[Type type]
        {
            get => _eventKeyRecords[type];
        }
    }
}