using System.Collections.Generic;

namespace AnyService
{
    public class RequestInfo
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public string RequesteeId { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Parameters { get; set; }
    }
}