using System;
using System.Linq;
using AnyService.Audity;
using AnyService.Core;
using AnyService.Core.Security;
using System.Threading.Tasks;

namespace AnyService.Services
{

    public class DefaultFilterFactory : IFilterFactory
    {
        #region fields
        private readonly WorkContext _workContext;
        private readonly IPermissionManager _permissionManager;
        #endregion

        public DefaultFilterFactory(WorkContext workContext, IPermissionManager permissionManager)
        {
            _workContext = workContext;
            _permissionManager = permissionManager;
        }
        public Func<object, Task<Func<TDomainModel, bool>>> GetFilter<TDomainModel>(string filterKey) where TDomainModel : IDomainModelBase
        {
            switch (filterKey)
            {
                case "__created": return _createdByUser<TDomainModel>();
                case "__updated": return _updatedByUser<TDomainModel>();
                case "__deleted": return _deletedByUser<TDomainModel>();
                case "__canread": return _canRead<TDomainModel>();
                case "__canupdate": return _canUpdate<TDomainModel>();
                case "__candelete": return _canDelete<TDomainModel>();
                case "__public": return _isPublic<TDomainModel>();

                default:
                    return null;
            }
        }
        private Func<object, Task<Func<TDomainModel, bool>>> _createdByUser<TDomainModel>()
        {
            return payload =>
            {
                return IsOfType<ICreatableAudit>() ?
                    Task.FromResult(new Func<TDomainModel, bool>(x => (x as ICreatableAudit).CreatedByUserId == _workContext.CurrentUserId)) :
                    null;
            };
        }
        private Func<object, Task<Func<TDomainModel, bool>>> _updatedByUser<TDomainModel>()
        {
            return payload =>
            {
                return IsOfType<IUpdatableAudit>() ?
                    Task.FromResult(new Func<TDomainModel, bool>(x => (x as IUpdatableAudit).UpdateRecords.Any(x => x.UpdatedByUserId == _workContext.CurrentUserId))) :
                    null;
            };
        }
        private Func<object, Task<Func<TDomainModel, bool>>> _deletedByUser<TDomainModel>()
        {
            return payload =>
            {
                return IsOfType<IDeletableAudit>() ?
                    Task.FromResult(new Func<TDomainModel, bool>(x => (x as IDeletableAudit).DeletedByUserId == _workContext.CurrentUserId)) :
                    null;
            };
        }
        private Func<object, Task<Func<TDomainModel, bool>>> _isPublic<TDomainModel>()
        {
            return payload =>
            {
                return IsOfType<IPublishable>() ?
                    Task.FromResult(new Func<TDomainModel, bool>(x => (x as IPublishable).Public)) :
                    null;
            };
        }
        private Func<object, Task<Func<TDomainModel, bool>>> _canRead<TDomainModel>()
        {
            return payload => throw new NotImplementedException(); ;
        }
        private Func<object, Task<Func<TDomainModel, bool>>> _canUpdate<TDomainModel>()
        {
            return payload => throw new NotImplementedException(); ;
        }
        private Func<object, Task<Func<TDomainModel, bool>>> _canDelete<TDomainModel>()
        {
            return payload => throw new NotImplementedException(); ;
        }

        private bool IsOfType<T>() => typeof(T).IsAssignableFrom(_workContext.CurrentType);
    }
}