namespace System
{
    public static class DateTimeExtnsions
    {
        public static string ToIso8601(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ssK");
        }
    }
}
