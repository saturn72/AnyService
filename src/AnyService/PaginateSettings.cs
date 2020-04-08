using System;
using System.Linq;

namespace AnyService
{
    public class PaginateSettings
    {
        private string _sortOrder;
        public const string Asc = "asc", Desc = "desc";
        public ulong DefaultOffset { get; set; }
        public ulong DefaultPageSize { get; set; }


        public string DefaultSortOrder
        {
            get => _sortOrder;
            set
            {
                value = value.ToLower();

                if (!new[] { Asc, Desc }.Contains(value))
                    throw new InvalidOperationException($"sortOrder must be {Asc} or {Desc}");
                _sortOrder = value;
            }
        }
    }
}