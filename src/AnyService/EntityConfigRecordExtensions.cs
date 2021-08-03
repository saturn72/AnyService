using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService
{
    public static class EntityConfigRecordExtensions
    {
        /// <summary>
        /// Gets EntityConfigRecord By Type
        /// </summary>
        /// <param name="records">All records</param>
        /// <param name="type">Types to search</param>
        /// <returns>matched EntityConfigRecord} or default</returns>
        public static EntityConfigRecord FirstOrDefault(this IEnumerable<EntityConfigRecord> records, Type type) =>
            records?.FirstOrDefault(r => r.Type == type);
        /// <summary>
        /// Gets EntityConfigRecord By Type. throws if not match any entity
        /// </summary>
        /// <param name="records">All records</param>
        /// <param name="type">Types to search</param>
        /// <returns>matched EntityConfigRecord} or default</returns>
        public static EntityConfigRecord First(this IEnumerable<EntityConfigRecord> records, Type type) =>
            records.First(r => r.Type == type);
        /// <summary>
        /// Gets all instances matches specific type
        /// </summary>
        /// <param name="records">All records</param>
        /// <param name="type">Types to search</param>
        /// <returns>IEnumerable of EntityConfigRecords</returns>
        public static IEnumerable<EntityConfigRecord> All(this IEnumerable<EntityConfigRecord> records, Type type) =>
            records?.Where(r => r.Type == type);
        /// <summary>
        /// Gets EntityConfigRecord By Type. throws if not match any entity
        /// </summary>
        /// <param name="records">All records</param>
        /// <param name="entityName">EntityConfigRecord to search name</param>
        /// <returns>matched EntityConfigRecord} or default</returns>
        public static EntityConfigRecord First(this IEnumerable<EntityConfigRecord> records, string entityName) =>
            records.First(r => r.Name == entityName);
    }
}