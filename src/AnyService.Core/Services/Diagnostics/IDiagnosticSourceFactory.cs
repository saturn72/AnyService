using System;
using System.Diagnostics;

namespace AnyService.Core.Services.Diagnostics
{
    public interface IDiagnosticSourceFactory
    {
        DiagnosticSource Get(string key);
        string GetEventPrefix(Type type);
    }
}
