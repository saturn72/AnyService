using System;

namespace AnyService
{
    public sealed class AnyServiceWorkContext
    {
        public string CurrentUserId { get; set; }
        public Type CurrentType { get; set; }
    }
}