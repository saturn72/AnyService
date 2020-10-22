using System;
using System.Linq;

namespace AnyService.Services
{
    public class PaginationSettings
    {
        public PaginationSettings()
        {
            DefaultOrderBy = nameof(IEntity.Id);
        }
        private string _sortOrder;
        private string _orderBy;
        public const string Asc = "asc", Desc = "desc";
        public int DefaultOffset { get; set; }
        public int DefaultPageSize { get; set; }
        public string DefaultOrderBy { get => _orderBy; set => _orderBy = value.Trim(); }
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