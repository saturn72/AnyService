using System.Linq.Expressions;

namespace System
{
    public static class FuncExtensions
    {
        public static Func<TDestination, TResult> Convert<TSource, TDestination, TResult>(this Func<TSource, TResult> func) where TDestination : TSource
        {
            return (TDestination x) => func((TDestination)x);
        }
        public static Expression<Func<TDestination, TResult>> Convert<TSource, TDestination, TResult>(this Expression<Func<TSource, TResult>> exp) where TDestination : TSource
        {
            var f = exp.Compile().Convert<TSource, TDestination, TResult>();
            return x => f(x);
        }
    }
}