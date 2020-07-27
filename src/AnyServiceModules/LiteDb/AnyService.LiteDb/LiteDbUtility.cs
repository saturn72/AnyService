using System;
using System.Linq;
using LiteDB;

namespace AnyService.LiteDb
{
    internal static class LiteDbUtility
    {
        public static IQueryable<T> Collection<T>(string dbName) => new LiteDatabase(dbName).GetCollection<T>().FindAll().AsQueryable();

        internal static void Command(string dbName, Action<LiteDatabase> command)
        {
            using var db = new LiteDatabase(dbName);
            command(db);
        }

        internal static TQueryResult Query<TQueryResult>(string dbName, Func<LiteDatabase, TQueryResult> query)
        {
            using var db = new LiteDatabase(dbName);
            return query(db);
        }
    }
}
