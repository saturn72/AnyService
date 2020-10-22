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
        public virtual Task<Func<object, Func<TDomainModel, bool>>> GetFilter<TDomainModel>(string filterKey) where TDomainModel : IEntity
        {
            return filterKey switch
            {
                "__canRead" => CanRead<TDomainModel>(),
                "__canUpdate" => CanUpdate<TDomainModel>(),
                "__canDelete" => CanDelete<TDomainModel>(),
                "__public" => IsPublic<TDomainModel>(),
                _ => Task.FromResult(null as Func<object, Func<TDomainModel, bool>>),
            };
        }
        protected virtual Task<Func<object, Func<TDomainModel, bool>>> IsPublic<TDomainModel>()
        {
            var isSoftDelete = IsOfType<ISoftDelete>();
            var isPublishable = IsOfType<IPublishable>();
            Func<object, Func<TDomainModel, bool>> p = payload =>
            {
                if (isPublishable && isSoftDelete)
                    return x => (x as IPublishable).Public && !(x as ISoftDelete).Deleted;
                return isPublishable ?
                    new Func<TDomainModel, bool>(x => (x as IPublishable).Public) :
                    null;
            };

            return Task.FromResult(p);
        }
        protected virtual async Task<Func<object, Func<TDomainModel, bool>>> CanRead<TDomainModel>() where TDomainModel : IEntity
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.ReadKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        protected virtual async Task<Func<object, Func<TDomainModel, bool>>> CanUpdate<TDomainModel>() where TDomainModel : IEntity
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.UpdateKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        protected virtual async Task<Func<object, Func<TDomainModel, bool>>> CanDelete<TDomainModel>() where TDomainModel : IEntity
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