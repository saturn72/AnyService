using System;
using System.Collections.Generic;
using System.Reflection;

namespace AnyService
{
    public interface ITypeFinder
    {
        IEnumerable<Type> GetAllTypesOf(Type assignedfromType, bool concretesOnly);
        IEnumerable<Type> GetAllTypesOf(Type assignedfromType, IEnumerable<Assembly> assemblies, bool concretesOnly);
    }
    public static class TypeFinderExtensions
    {
        public static IEnumerable<Type> FindAllTypesOf<TType>(this ITypeFinder typeFinder, bool concretesOnly = true) =>
            typeFinder.GetAllTypesOf(typeof(TType), concretesOnly);
    }
}
