using System;
using System.Linq;
using AnyService.Audity;
using AnyService.Security;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public class DefaultFilterFactory : IFilterFactory
    {
        #region fields
        private readonly WorkContext _workContext;
        private readonly IPermissionManager _permissionManager;
        #endregion

        public DefaultFilterFactory(
            WorkContext workContext, 
            IPermissionManager permissionManager)
        {
            _workContext = workContext;
            _permissionManager = permissionManager;
        }
        public virtual Task<Func<object, Func<TEntity, bool>>> GetFilter<TEntity>(string filterKey) where TEntity : IEntity
        {
            return filterKey switch
            {
                "__canRead" => CanRead<TEntity>(),
                "__canUpdate" => CanUpdate<TEntity>(),
                "__canDelete" => CanDelete<TEntity>(),
                "__public" => IsPublic<TEntity>(),
                _ => Task.FromResult(null as Func<object, Func<TEntity, bool>>),
            };
        }
        protected virtual Task<Func<object, Func<TEntity, bool>>> IsPublic<TEntity>()
        {
            var isSoftDelete = IsOfType<ISoftDelete>();
            var isPublishable = IsOfType<IPublishable>();
            Func<object, Func<TEntity, bool>> p = payload =>
            {
                if (isPublishable && isSoftDelete)
                    return x => (x as IPublishable).Public && !(x as ISoftDelete).Deleted;
                return isPublishable ?
                    new Func<TEntity, bool>(x => (x as IPublishable).Public) :
                    null;
            };

            return Task.FromResult(p);
        }
        protected virtual async Task<Func<object, Func<TEntity, bool>>> CanRead<TEntity>() where TEntity : IEntity
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.ReadKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        protected virtual async Task<Func<object, Func<TEntity, bool>>> CanUpdate<TEntity>() where TEntity : IEntity
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.UpdateKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        protected virtual async Task<Func<object, Func<TEntity, bool>>> CanDelete<TEntity>() where TEntity : IEntity
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.DeleteKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        private bool IsOfType<T>() => typeof(T).IsAssignableFrom(_workContext.CurrentType);
    }
}