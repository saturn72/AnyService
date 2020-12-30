using AnyService.Core.Services.Diagnostics;
using Shouldly;
using System.Diagnostics;
using Xunit;

namespace AnyService.Tests.CoreServices.Diagnostics
{
    public class DiagnosticSourceFactoryTests
    {
        [Fact]
        public void Get_OnTraceDisabled_CreatesDummyDiagnosticSource()
        {
            var dsf = new DiagnosticSourceFactory(false);
            var l = dsf.Get("some-key").ShouldBeOfType<DiagnosticSourceFactory.DummyDiagnosticSource>();
            l.IsEnabled("fff").ShouldBeFalse();
        }


        [Fact]
        public void Get_OnTraceEnabled_CreatesDiagnosticListener()
        {
            var k = "key";
            var dsf = new DiagnosticSourceFactory(true);
            var l = dsf.Get(k).ShouldBeOfType<DiagnosticListener>();
            l.Name.ShouldBe(k);
        }
        [Fact]
        public void GetEventPrefix()
        {
            var dsf = new DiagnosticSourceFactory(true);
            dsf.GetEventPrefix(typeof(string)).ShouldBe("System.String");
        }
    }
}
