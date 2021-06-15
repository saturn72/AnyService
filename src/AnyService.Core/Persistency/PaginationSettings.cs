using System;
using System.ComponentModel;
using System.Linq;

namespace AnyService.Services
{
    public class PaginationSettings
    {
        public PaginationSettings()
        {
            DefaultOrderBy = nameof(IEntity.Id);
            DefaultSortOrder = Asc;
        }
        private string _sortOrder;
        private string _orderBy;

        public const string Asc = "asc", Desc = "desc";
        [DefaultValue(0)]
        public int DefaultOffset { get; set; } = 0;
        [DefaultValue(50)]
        public int DefaultPageSize { get; set; } = 50;
        [DefaultValue(nameof(IEntity.Id))]
        public string DefaultOrderBy { get => _orderBy; set => _orderBy = value.Trim(); }
        public bool CaseSensitiveOrderBy { get; set; }
        [DefaultValue(Asc)]
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