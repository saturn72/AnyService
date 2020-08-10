namespace AnyService.Infrastructure
{
    public interface IAppEngine
    {
        TService GetService<TService>();
    }
}
