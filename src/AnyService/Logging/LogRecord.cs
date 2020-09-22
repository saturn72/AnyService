﻿using System;

namespace AnyService.Logging
{

    public class LogRecord : IDomainModelBase
    {
        public string Id { get; set; }
        public string Level { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public string ExceptionId { get; set; }
        public string ExceptionRuntimeType { get; set; }
        public string ExceptionRuntimeMessage { get; set; }
        public string Message { get; set; }
        public string IpAddress { get; set; }
        public string RequestPath { get; set; }
        public string RequestHeaders { get; set; }
        public string HttpMethod { get; set; }
        public string Request { get; set; }
        public string WorkContext { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}