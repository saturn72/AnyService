using Shouldly;
using System;
using Xunit;

namespace AnyService.Tests.Services.Diagnostics
{
    public class DiagnosticSourceExtensionsTests
    {
        [Fact]
        public void GetEventPrefix()
        {
            DiagnosticSourceExtensions.GetEventPrefix(typeof(string)).ShouldBe("System.String");
        }
    }
}
