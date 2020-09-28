﻿using System.Collections.Generic;

namespace AnyService.Models
{
    public interface IParentApiModel<TDomainModel> where TDomainModel : IDomainObject
    {
        public string ChildEntityKey { get; }
        public IEnumerable<TDomainModel> Childs { get; set; }
    }
}
