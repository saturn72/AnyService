using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AnyService.Services
{
    public class Pagination<TDomainModel>
    {
        public Pagination()
        {
            SortOrder = PaginationSettings.Asc;
        }
        public Pagination(string queryOrFilter) : this()
        {
            QueryOrFilter = queryOrFilter;
        }
        public Pagination(Expression<Func<TDomainModel, bool>> queryFunc) : this()
        {
            QueryFunc = queryFunc?.Compile();
            QueryOrFilter = queryFunc.ToString();
        }
        /// <summary>
        /// Gets or sets the total number of entities 
        /// </summary>
        /// <value>ulong</value>
        public int Total { get; set; }
        /// <summary>
        /// Gets or sets current page offset
        /// default vlue is 0
        /// </summary>
        /// <value>ulong</value>
        public int Offset { get; set; } = 0;
        /// <summary>
        /// Gets or sets current page size
        /// default value is 50
        /// </summary>
        /// <value>ulong</value>
        public int PageSize { get; set; } = 50;
        public string SortOrder { get; set; }
        /// <summary>
        /// Gets or sets value indicating sort by property name.
        /// Default value is 'Id'
        /// </summary>
        /// <value></value>
        public string OrderBy { get; set; } = nameof(IEntity.Id);
        /// <summary>
        /// Gets or sets current page data
        /// </summary>
        /// <value>ulong</value>
        public IEnumerable<TDomainModel> Data { get; set; }

        /// <summary>
        /// gets or sets query string value
        /// </summary>
        /// <value></value>
        public string QueryOrFilter { get; set; }
        /// <summary>
        /// gets or sets query func
        /// </summary>
        /// <value></value>
        public Func<TDomainModel, bool> QueryFunc { get; set; }
        public bool IncludeNested { get; set; }
    }
}