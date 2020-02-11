using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService
{
    public class EntityConfigRecordManager
    {
        private static IEnumerable<EntityConfigRecord> _records;
        private static IDictionary<Type, EntityConfigRecord> _recordDictionary;

        public static EntityConfigRecord GetRecord(Type type) => _recordDictionary[type];

        public static IEnumerable<EntityConfigRecord> EntityConfigRecords
        {
            get => _records;
            set
            {
                _records = value;
                _recordDictionary = _records.ToDictionary(k => k.Type, v => v);
            }
        }
    }
}
