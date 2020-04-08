namespace AnyService.Utilities
{
    public static class IdGeneratorExtensions
    {
        public static T GetNext<T>(this IIdGenerator generator)
        {
            return (T)generator.GetNext();
        }
    }
}