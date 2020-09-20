using AnyService.Logging;
using System;
using System.Collections.Generic;

namespace AnyService.Services.Logging
{
    public class LogRecordPagination : Pagination<LogRecord>
    {
        public IEnumerable<string> LogRecordIds { get; set; }
        public IEnumerable<string> LogLevels { get; set; }
        public IEnumerable<string> UserIds { get; set; }
        public IEnumerable<string> ClientIds { get; set; }
        public IEnumerable<string> ExceptionIds { get; set; }
        public IEnumerable<string> ExceptionRuntimeTypes { get; set; }
        public IEnumerable<string> IpAddresses { get; set; }
        public IEnumerable<string> HttpMethods { get; set; }
        public IEnumerable<string> ExceptionRuntimeMessages { get; set; }
        public IEnumerable<string> ExceptionRuntimeMessageContains { get; set; }
        public IEnumerable<string> Messages { get; set; }
        public IEnumerable<string> MessageContains { get; set; }
        public IEnumerable<string> RequestPaths { get; set; }
        public IEnumerable<string> RequestPathContains { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
    }
}
