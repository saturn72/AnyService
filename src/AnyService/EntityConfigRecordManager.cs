using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService
{
    public class EntityConfigRecordManager
    {
        private IEnumerable<EntityConfigRecord> _records;
        private IDictionary<Type, EntityConfigRecord> _recordDictionary;

        public EntityConfigRecord GetRecord(Type type) => _recordDictionary[type];

        public IEnumerable<EntityConfigRecord> EntityConfigRecords
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
