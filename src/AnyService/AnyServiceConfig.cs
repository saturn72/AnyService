using System.Collections.Generic;

namespace AnyService
{
    public sealed class AnyServiceConfig
    {
        public AnyServiceConfig()
        {
            MaxMultipartBoundaryLength = 50;
            MaxValueCount = 25;
            ManageEntityPermissions = true;
        }

        public IEnumerable<TypeConfigRecord> TypeConfigRecords { get; set; }
        public bool ManageEntityPermissions { get; set; }
        public int MaxMultipartBoundaryLength { get; set; }
        public int MaxValueCount { get; set; }
    }
}