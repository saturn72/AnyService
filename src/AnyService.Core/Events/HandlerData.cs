using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class HandlerData<TEvent>
    {
        public HandlerData(string @namspace, string eventKey)
        {
            Namespace = namspace;
            EventKey = eventKey;
        }
        public string HandlerId { get; set; }
        public string EventKey { get; }
        public string Namespace { get; }
        public string Alias { get; set; }
        public Func<TEvent, IServiceProvider, Task> Handler { get; set; }
    }
}