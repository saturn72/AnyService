using System.Collections.Generic;

namespace AnyService.Models
{

    public class PaginationModel<T>
    {
        public ulong Total { get; set; }
        public ulong Offset { get; set; }
        public ulong PageSize { get; set; }
        public string SortOrder { get; set; }
        public string OrderBy { get; set; }
        public IEnumerable<T> Data { get; set; }
        public string Query { get; set; }
    }
}
