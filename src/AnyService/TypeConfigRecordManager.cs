using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService
{
    public class TypeConfigRecordManager
    {
        private static IEnumerable<TypeConfigRecord> _records;
        private static IDictionary<Type, TypeConfigRecord> _recordDictionary;

        public static TypeConfigRecord GetRecord(Type type) => _recordDictionary[type];

        public static IEnumerable<TypeConfigRecord> TypeConfigRecords
        {
            get => _records;
            internal set
            {
                _records = value;
                _recordDictionary = _records.ToDictionary(k => k.Type, v => v);
            }
        }
    }
}
