using System;

namespace AnyService
{
    public class WorkContext : ExtendableBase
    {
        public Type CurrentType => CurrentEntityConfigRecord?.Type;
        public EntityConfigRecord CurrentEntityConfigRecord { get; set; }
        public string TraceId
        {
            get => GetParameterOrDefault<string>(nameof(TraceId));
            set => SetParameter(nameof(TraceId), value);
        }
        public string SpanId
        {
            get => GetParameterOrDefault<string>(nameof(SpanId));
            set => SetParameter(nameof(SpanId), value);
        }
        public string CurrentUserId
        {
            get => GetParameterOrDefault<string>(nameof(CurrentUserId));
            set => SetParameter(nameof(CurrentUserId), value);
        }
        public string CurrentClientId
        {
            get => GetParameterOrDefault<string>(nameof(CurrentClientId));
            set => SetParameter(nameof(CurrentClientId), value);
        }
        public RequestInfo RequestInfo { get; set; }
        public string IpAddress
        {
            get => GetParameterOrDefault<string>(nameof(IpAddress));
            set => SetParameter(nameof(IpAddress), value);
        }
        public string SessionId
        {
            get => GetParameterOrDefault<string>(nameof(SessionId));
            set => SetParameter(nameof(SessionId), value);
        }
        public string ReferenceId
        {
            get => GetParameterOrDefault<string>(nameof(ReferenceId));
            set => SetParameter(nameof(ReferenceId), value);
        }
    }
}