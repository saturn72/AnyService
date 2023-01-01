namespace AnyService.Tests.Services.Diagnostics
{
    public class DiagnosticSourceExtensionsTests
    {
        [Fact]
        public void GetEventPrefix()
        {
            DiagnosticSourceExtensions.GetListenerName(typeof(string)).ShouldBe("System.String");
        }
    }
}
