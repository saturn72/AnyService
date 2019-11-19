using System;

namespace AnyService
{
    public sealed class WorkContext
    {
        public Type CurrentType { get; set; }
        public string CurrentUserId { get; set; }
    }
}