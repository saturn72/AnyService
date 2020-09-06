namespace AnyService.Tests.Controllers
{
    public class ControllerTestBase
    {
        public ControllerTestBase()
        {
            MappingExtensions.Configure(x => { });
        }
    }
}