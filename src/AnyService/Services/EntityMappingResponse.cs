using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public class EntityMappingResponse<TParent, TChild> 
        where TParent:IDomainEntity
        where TChild :IDomainEntity
    {
    }
}
