using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class HandlerData
    {
        public string HandlerId { get; set; }
        public string Name { get; set; }
        public Func<Event, IServiceProvider, Task> Handler { get; set; }
    }
}