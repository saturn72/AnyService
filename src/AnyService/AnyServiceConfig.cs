using System;
using System.Linq;
using System.Collections.Generic;
using AnyService.Audity;
using AnyService.Core;
using AnyService.Services;
using System.Linq.Expressions;

namespace AnyService
{
    public sealed class AnyServiceConfig
    {
        public AnyServiceConfig()
        {
            MaxMultipartBoundaryLength = 50;
            MaxValueCount = 25;
            ManageEntityPermissions = true;
            UseAuthorizationMiddleware = true;
            UseExceptionLogging = true;
            DefaultPaginationSettings = new PaginationSettings
            {
                DefaultOrderBy = nameof(IDomainModelBase.Id),
                DefaultOffset = 1,
                DefaultPageSize = 50,
                DefaultSortOrder = PaginationSettings.Asc,
            };
            GetAllQueries = new DefaultGetAllQueries().Queries;
        }

        public IEnumerable<EntityConfigRecord> EntityConfigRecords { get; set; }

        /// <summary>
        /// Gets or sets value indicating whether anyservice manages user permission on entity. 
        /// default value = true
        /// </summary>
        /// <value></value>
        public bool ManageEntityPermissions { get; set; }
        public bool UseAuthorizationMiddleware { get; set; }
        public bool UseExceptionLogging { get; set; }
        public int MaxMultipartBoundaryLength { get; set; }
        public int MaxValueCount { get; set; }
        public PaginationSettings DefaultPaginationSettings { get; set; }

        public IReadOnlyDictionary<string, Func<object, LambdaExpression>> GetAllQueries { get; set; }

        #region nested classes
        private class DefaultGetAllQueries
        {
            internal IReadOnlyDictionary<string, Func<object, LambdaExpression>> Queries { get; private set; }
            internal DefaultGetAllQueries()
            {
                var rq = new Dictionary<string, Func<object, LambdaExpression>>()
                {
                    {"__created",  _createdByUser()},
                    {"__updated", _updatedByUser()},
                    {"__deleted", _deletedByUser()},
                    {"__public", _isPublic()},
                };
                Queries = rq;
            }

            private Func<object, LambdaExpression> _createdByUser()
            {
                return payload =>
                {
                    var type = payload.GetPropertyValueByName<Type>("Type");
                    var userId = payload.GetPropertyValueByName<string>("UserId");
                    return ExpressionTreeBuilder.BuildBinaryTreeExpression(type, $"{nameof(ICreatableAudit.CreatedByUserId)} == {userId}");
                    // if (IsOfType<ICreatableAudit>(type))
                    // {
                    //     Func<IDomainModelBase, bool> f = x =>
                    //     {
                    //         var r = (x as ICreatableAudit).CreatedByUserId == userId;
                    //         return r;
                    //     };
                    //     Expression<Func<IDomainModelBase, bool>> exp = i => f(i);
                    //     return exp;
                    // }
                    // return null;
                };
            }
            private Func<object, LambdaExpression> _deletedByUser()
            {
                return payload =>
               {
                   var type = payload.GetPropertyValueByName<Type>("Type");
                   var userId = payload.GetPropertyValueByName<string>("UserId");
                   if (IsOfType<IDeletableAudit>(type))
                   {
                       Func<object, bool> f = x => (x as IDeletableAudit).DeletedByUserId == userId;
                       Expression<Func<IDomainModelBase, bool>> exp = i => f(i);
                       return exp;
                   }
                   return null;
               };
            }
            private Func<object, LambdaExpression> _updatedByUser()
            {
                return payload =>
                {
                    var type = payload.GetPropertyValueByName<Type>("Type");
                    var userId = payload.GetPropertyValueByName<string>("UserId");
                    if (IsOfType<IUpdatableAudit>(type))
                    {
                        Func<object, bool> f = x => (x as IUpdatableAudit).UpdateRecords.Any(u => u.UpdatedByUserId == userId);
                        Expression<Func<IDomainModelBase, bool>> exp = i => f(i);
                        return exp;
                    }
                    return null;
                };
            }
            private Func<object, LambdaExpression> _isPublic()
            {
                return payload =>
                {
                    var type = payload.GetPropertyValueByName<Type>("Type");
                    if (IsOfType<IPublishable>(type))
                    {
                        Func<object, bool> f = x => (x as IPublishable).Public;
                        Expression<Func<IDomainModelBase, bool>> exp = i => f(i);
                        return exp;
                    }
                    return null;
                };
            }

            private bool IsOfType<T>(Type type) => typeof(T).IsAssignableFrom(type);
        }
        #endregion
    }
}