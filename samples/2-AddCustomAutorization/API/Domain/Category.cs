﻿using AnyService;

namespace API.Domain
{
    public class Category : IDomainModelBase
    {
        public string Id {get;set;}
        public string Name { get; set; }
    }
}
