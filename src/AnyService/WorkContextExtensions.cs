using System.Collections.Generic;
using System.Linq;

namespace AnyService
{
    public static class WorkContextExtensions
    {
        public static T GetFirstParameter<T>(this WorkContext workContext, string parameterName)
        {
            return GetParameterByIndex<T>(workContext, parameterName, 0);
        }
        public static T GetParameterByIndex<T>(this WorkContext workContext, string parameterName, int index)
        {
            if (!workContext.Parameters.TryGetValue(parameterName, out object value))
                return default;
            return (value as IEnumerable<T>).ElementAt(index);
        }
    }
}