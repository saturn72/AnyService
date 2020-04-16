using System;
using System.Collections.Generic;
using AnyService.Core;

namespace AnyService.Services
{
    public class Paginate<TDomainModel> where TDomainModel : IDomainModelBase
    {
        public Paginate()
        {
            SortOrder = PaginateSettings.Asc;
        }
        public Paginate(Func<TDomainModel, bool> query) : this()
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
        /// Gets or sets current page data
        /// </summary>
        /// <value>ulong</value>
        public IEnumerable<TDomainModel> Data { get; set; }

        /// <summary>
        /// gets or sets query
        /// </summary>
        /// <value></value>
        public Func<TDomainModel, bool> Query { get; set; }
    }
}