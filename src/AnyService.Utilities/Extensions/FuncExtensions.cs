using System.Linq;
using System.Linq.Expressions;

namespace System
{
    public static class FuncExtensions
    {
        public static Func<T, bool> AndAlso<T>(this Func<T, bool> func1, Func<T, bool> func2, params Func<T, bool>[] args)
        {
            var funcList = (args ?? new Func<T, bool>[] { }).ToList();
            funcList.Insert(0, func1);
            funcList.Insert(1, func2);
            funcList = funcList.Where(x => x != null).ToList();
            if (funcList.Count() > 1)
                return funcList?.Aggregate((first, second) => x => first(x) && second(x));
            return funcList.Any() ? funcList.ElementAt(0) : null;
        }
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