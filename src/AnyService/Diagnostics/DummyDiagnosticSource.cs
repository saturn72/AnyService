using System.Diagnostics;

namespace AnyService.Diagnostics
{
    public class DummyDiagnosticSource : DiagnosticSource
    {
        public override bool IsEnabled(string name) => false;
        public override void Write(string name, object value)
        {
        }
    }
}
