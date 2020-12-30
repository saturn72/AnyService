using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AnyService.Core.Services.Diagnostics
{
    public class DiagnosticSourceFactory : IDiagnosticSourceFactory
    {
        private readonly ConcurrentDictionary<string, DiagnosticSource> _sources;
        private readonly bool _traceEnabled;
        private readonly DiagnosticSource _dummyDiagnosticSource;

        public DiagnosticSourceFactory(bool traceEnabled)
        {
            _traceEnabled = traceEnabled;
            if (_traceEnabled)
                _sources = new ConcurrentDictionary<string, DiagnosticSource>(StringComparer.InvariantCultureIgnoreCase);
            else
                _dummyDiagnosticSource = new DummyDiagnosticSource();
        }
        public DiagnosticSource Get(string key)
        {
            if (!_traceEnabled) return _dummyDiagnosticSource;

            if (!_sources.TryGetValue(key, out DiagnosticSource value))
            {
                value = new DiagnosticListener(key);
                _sources.TryAdd(key, value);
            }
            return value;
        }

        public string GetEventPrefix(Type type) => $"{GetType().Namespace}.{GetType().Name}";

        public class DummyDiagnosticSource : DiagnosticSource
        {
            public override bool IsEnabled(string name) => false;
            public override void Write(string name, object value)
            {
            }
        }
    }
}
