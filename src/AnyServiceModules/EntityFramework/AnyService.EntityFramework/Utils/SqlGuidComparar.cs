using System.Data.SqlTypes;

namespace AnyService.EntityFramework
{
    public static class SqlGuidComparar
    {
        public static int CompareTo(this string sqlGuidStringA, string sqlGuidStringB) => CompareTo(new SqlGuid(sqlGuidStringA), new SqlGuid(sqlGuidStringB));
        public static int CompareTo(this SqlGuid sqlGuidA, SqlGuid sqlGuidB) => sqlGuidA.CompareTo(sqlGuidB);
    }
}
