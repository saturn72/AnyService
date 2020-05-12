using System;
using System.Linq;
using AnyService.Audity;
using AnyService.Core;
using AnyService.Core.Security;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public Task<Func<object, Func<TDomainModel, bool>>> GetFilter<TDomainModel>(string filterKey) where TDomainModel : IDomainModelBase
        {
            switch (filterKey)
            {
                case "__created": return _createdByUser<TDomainModel>();
                case "__updated": return _updatedByUser<TDomainModel>();
                case "__deleted": return _deletedByUser<TDomainModel>();
                case "__canRead": return _canRead<TDomainModel>();
                case "__canUpdate": return _canUpdate<TDomainModel>();
                case "__canDelete": return _canDelete<TDomainModel>();
                case "__public": return _isPublic<TDomainModel>();

                default:
                    return Task.FromResult(null as Func<object, Func<TDomainModel, bool>>);
            }
        }
        private Task<Func<object, Func<TDomainModel, bool>>> _createdByUser<TDomainModel>() where TDomainModel : IDomainModelBase
        {
            var userId = _workContext.CurrentUserId;

            Func<object, Func<TDomainModel, bool>> p = payload =>
            {
                var s = IsOfType<ICreatableAudit>();
                var e = ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>($"{nameof(ICreatableAudit.CreatedByUserId)} == {userId}")?.Compile();

                return IsOfType<ICreatableAudit>() ?
                    ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>($"{nameof(ICreatableAudit.CreatedByUserId)} == {userId}")?.Compile() :
                    null;
            };

            return Task.FromResult(p);
        }
        private Task<Func<object, Func<TDomainModel, bool>>> _updatedByUser<TDomainModel>()
        {
            var userId = _workContext.CurrentUserId;
            Func<object, Func<TDomainModel, bool>> p = payload =>
            {
                return IsOfType<IUpdatableAudit>() ?
                    new Func<TDomainModel, bool>(x =>
                    {
                        var updaterecords = (x as IUpdatableAudit).UpdateRecords;
                        return !updaterecords.IsNullOrEmpty() && updaterecords.Any(x => x.UpdatedByUserId == userId);
                    }) :
                    null;
            };
            return Task.FromResult(p);
        }
        private Task<Func<object, Func<TDomainModel, bool>>> _deletedByUser<TDomainModel>()
        {
            var userId = _workContext.CurrentUserId;
            Func<object, Func<TDomainModel, bool>> p = payload =>
            {
                return IsOfType<IDeletableAudit>() ?
                    ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>($"{nameof(IDeletableAudit.DeletedByUserId)} == {userId}")?.Compile() :
                    null;
            };
            return Task.FromResult(p);
        }
        private Task<Func<object, Func<TDomainModel, bool>>> _isPublic<TDomainModel>()
        {
            var isDeletable = IsOfType<IDeletableAudit>();
            var isPublishable = IsOfType<IPublishable>();
            Func<object, Func<TDomainModel, bool>> p = payload =>
            {
                if (isPublishable && isDeletable)
                    return x => (x as IPublishable).Public && !(x as IDeletableAudit).Deleted;
                return isPublishable ?
                    ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>($"{nameof(IPublishable.Public)} == {true}")?.Compile() :
                    null;
            };

            return Task.FromResult(p);
        }
        private async Task<Func<object, Func<TDomainModel, bool>>> _canRead<TDomainModel>() where TDomainModel : IDomainModelBase
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.ReadKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        private async Task<Func<object, Func<TDomainModel, bool>>> _canUpdate<TDomainModel>() where TDomainModel : IDomainModelBase
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.UpdateKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        private async Task<Func<object, Func<TDomainModel, bool>>> _canDelete<TDomainModel>() where TDomainModel : IDomainModelBase
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