namespace System
{
    public static class TypeExtensions
    {
        public static bool IsOfOpenGenericType(this Type type, Type openGenericType)
        {
            try
            {
                var genericTypeDefinition = openGenericType.GetGenericTypeDefinition();
                //if of same generic type definitions
                if (genericTypeDefinition.IsAssignableFrom(type.GetGenericTypeDefinition()))
                    return true;

                //check interfaces
                foreach (var implementedInterface in type.FindInterfaces((objType, objCriteria) => true, null))
                {
                    if (!implementedInterface.IsGenericType)
                        continue;

                    if (genericTypeDefinition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition()))
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
