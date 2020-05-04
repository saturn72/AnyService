namespace System
{
    public static class FuncExtensions
    {
        public static Func<TDestination, TResult> Convert<TSource, TDestination, TResult>(this Func<TSource, TResult> func) where TDestination : TSource
        {
            return (TDestination x) => func((TDestination)x);
        }
    }
}