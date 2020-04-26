using System;
using System.Collections.Generic;
using AnyService.Core;

namespace AnyService.Services
{
    public class Pagination<TDomainModel> where TDomainModel : IDomainModelBase
    {
        public Pagination()
        {
            SortOrder = PaginationSettings.Asc;
        }
        public Pagination(string query) : this()
        {
            Query = query;
        }
        /// <summary>
        /// Gets or sets the total number of entities 
        /// </summary>
        /// <value>ulong</value>
        public ulong Total { get; set; }
        /// <summary>
        /// Gets or sets current page offset
        /// </summary>
        /// <value>ulong</value>
        public ulong Offset { get; set; }
        /// <summary>
        /// Gets or sets current page size
        /// </summary>
        /// <value>ulong</value>
        public ulong PageSize { get; set; }
        public string SortOrder { get; set; }
        /// <summary>
        /// Gets or sets value indicating sort by property name
        /// </summary>
        /// <value></value>
        public string OrderBy { get; set; }
        /// <summary>
        /// Gets or sets current page data
        /// </summary>
        /// <value>ulong</value>
        public IEnumerable<TDomainModel> Data { get; set; }

        /// <summary>
        /// gets or sets query
        /// </summary>
        /// <value></value>
        public string Query { get; set; }
    }
}