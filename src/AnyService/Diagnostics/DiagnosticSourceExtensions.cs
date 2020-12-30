namespace System
{
    public static class DiagnosticSourceExtensions
    {
        public static string GetEventPrefix(this Type type) => $"{type.Namespace}.{type.Name}";
    }
}
