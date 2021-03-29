using System.Collections.Generic;

namespace AnyService
{
    public class RequestInfo : ExtendableBase
    {
        public string Path
        {
            get => GetParameterOrDefault<string>(nameof(Path));
            set => SetParameter(nameof(Path), value);
        }
        public string Method
        {
            get => GetParameterOrDefault<string>(nameof(Method));
            set => SetParameter(nameof(Method), value);
        }
        public string RequesteeId
        {
            get => GetParameterOrDefault<string>(nameof(RequesteeId));
            set => SetParameter(nameof(RequesteeId), value);
        }
    }
}