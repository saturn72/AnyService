using System;

namespace AnyService
{
    public sealed class WorkContext
    {
        public string CurrentUserId { get; set; }
        public Type CurrentType { get; set; }
    }
}