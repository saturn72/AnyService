using System;
using LiteDB;

namespace AnyService.LiteDb
{
    internal static class LiteDbUtility
    {
        internal static void Command(string dbName, Action<LiteDatabase> command)
        {
            using (var db = new LiteDatabase(dbName))
            {
                command(db);
            }
        }

        internal static TQueryResult Query<TQueryResult>(string dbName, Func<LiteDatabase, TQueryResult> query)
        {
            using (var db = new LiteDatabase(dbName))
            {
                return query(db);
            }
        }
    }
}
