using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AnyService.Core;

namespace AnyService.Services
{
    public class Pagination<TDomainModel> where TDomainModel : IDomainModelBase
    {
        public Pagination()
        {
            SortOrder = PaginationSettings.Asc;
        }
        public Pagination(string queryAsString) : this()
        {
            QueryAsString = queryAsString;
            QueryFunc = ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>(queryAsString);
        }
        public Pagination(Expression<Func<TDomainModel, bool>> queryFunc) : this()
        {
            QueryFunc = queryFunc;
            QueryAsString = queryFunc.ToString();
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
        /// gets or sets query string value
        /// </summary>
        /// <value></value>
        public string QueryAsString { get; set; }
        /// <summary>
        /// gets or sets query func
        /// </summary>
        /// <value></value>
        public Expression<Func<TDomainModel, bool>> QueryFunc { get; set; }
    }
    public class PaginationApiModel<T>
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