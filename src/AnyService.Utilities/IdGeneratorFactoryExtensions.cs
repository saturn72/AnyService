namespace AnyService.Utilities
{
    public static class IdGeneratorFactoryExtensions
    {
        public static IIdGenerator GetGenerator<T>(this IdGeneratorFactory factory) => factory.GetGenerator(typeof(T));
        public static T GetNext<T>(this IdGeneratorFactory factory) => IdGeneratorFactoryExtensions.GetGenerator<T>(factory).GetNext<T>();
    }
}