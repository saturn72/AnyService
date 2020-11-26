﻿using AnyService.Audity;

namespace AnyService.SampleApp.Models
{
    public class MyModel : IEntity, IFullAudit, IPublishable, ISoftDelete
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public bool Deleted { get; set; }
        public bool Public { get; set; }
    }
}