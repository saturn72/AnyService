namespace System
{
    public static class DiagnosticSourceExtensions
    {
        public static string GetListenerName(this Type type) => $"{type.Namespace}.{type.Name}";
    }
}
