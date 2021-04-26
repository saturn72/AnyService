﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Events
{
    public interface ISubscriptionManager<TEvent>
    {
        Task<string> Subscribe(string @namespace, string eventKey, Func<TEvent, IServiceProvider, Task> handler, string alias);
        Task Unsubscribe(string handlerId);
        Task<HandlerData<TEvent>> GetByHandlerId(string handlerId);
        Task<IEnumerable<HandlerData<TEvent>>> GetAllHandlers();
        Task<IEnumerable<HandlerData<TEvent>>> GetHandlers(string @namespace, string eventKey);
        void Clear();
    }
}