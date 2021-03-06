using System;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public class HandlerData<TEvent>
    {
        public string HandlerId { get; set; }
        public string Name { get; set; }
        public Func<TEvent, IServiceProvider, Task> Handler { get; set; }
    }
}