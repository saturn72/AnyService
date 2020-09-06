using System;
using System.Linq;
using AnyService.Audity;
using AnyService.Security;
using System.Threading.Tasks;
using System.Collections.Generic;
using AnyService.Services.Audit;

namespace AnyService.Services
{
    public class DefaultFilterFactory : IFilterFactory
    {
        #region fields
        private readonly WorkContext _workContext;
        private readonly IPermissionManager _permissionManager;
        private readonly IRepository<AuditRecord> _auditRepository;
        #endregion

        public DefaultFilterFactory(
            WorkContext workContext, 
            IPermissionManager permissionManager,
            IRepository<AuditRecord> auditRepository)
        {
            _workContext = workContext;
            _permissionManager = permissionManager;
            _auditRepository = auditRepository;
        }
        public virtual Task<Func<object, Func<TDomainModel, bool>>> GetFilter<TDomainModel>(string filterKey) where TDomainModel : IDomainModelBase
        {
            return filterKey switch
            {
                "__created" => CreatedByUser<TDomainModel>(),
                "__updated" => UpdatedByUser<TDomainModel>(),
                "__deleted" => DeletedByUser<TDomainModel>(),
                "__canRead" => CanRead<TDomainModel>(),
                "__canUpdate" => CanUpdate<TDomainModel>(),
                "__canDelete" => CanDelete<TDomainModel>(),
                "__public" => IsPublic<TDomainModel>(),
                _ => Task.FromResult(null as Func<object, Func<TDomainModel, bool>>),
            };
        }
        protected virtual Task<Func<object, Func<TDomainModel, bool>>> CreatedByUser<TDomainModel>() where TDomainModel : IDomainModelBase
        {
            Func<object, Func<TDomainModel, bool>> p = payload =>
            {
            if (!IsOfType<ICreatableAudit>()) return null;
            var q =   new Func<TDomainModel, bool>(x =>
                (x.UserId == _workContext.CurrentUserId || x.ClientId == _workContext.CurrentClientId)
                && x.EntityName == auditEntityName(typeof(TDomainModel)
                && x.AuditRecordType == AuditRecordTypes.CREATE)


                    ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>($"{nameof(ICreatableAudit.CreatedByUserId)} == {userId}")?.Compile() :
                    null;
            };

            return Task.FromResult(p);
        }
        protected virtual Task<Func<object, Func<TDomainModel, bool>>> UpdatedByUser<TDomainModel>()
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
        protected virtual Task<Func<object, Func<TDomainModel, bool>>> DeletedByUser<TDomainModel>()
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
        protected virtual Task<Func<object, Func<TDomainModel, bool>>> IsPublic<TDomainModel>()
        {
            var isDeletable = IsOfType<IDeletableAudit>();
            var isPublishable = IsOfType<IPublishable>();
            Func<object, Func<TDomainModel, bool>> p = payload =>
            {
                if (isPublishable && isDeletable)
                    return x => (x as IPublishable).Public && !(x as ISoftDelete).Deleted;
                return isPublishable ?
                    ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>($"{nameof(IPublishable.Public)} == {true}")?.Compile() :
                    null;
            };

            return Task.FromResult(p);
        }
        protected virtual async Task<Func<object, Func<TDomainModel, bool>>> CanRead<TDomainModel>() where TDomainModel : IDomainModelBase
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.ReadKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        protected virtual async Task<Func<object, Func<TDomainModel, bool>>> CanUpdate<TDomainModel>() where TDomainModel : IDomainModelBase
        {
            var ecr = _workContext.CurrentEntityConfigRecord;
            var permittedIds = await _permissionManager.GetPermittedIds(
                _workContext.CurrentUserId,
                ecr.EntityKey,
                ecr.PermissionRecord.UpdateKey);
            return payload => a => permittedIds.Any(x => x == a.Id);
        }
        protected virtual async Task<Func<object, Func<TDomainModel, bool>>> CanDelete<TDomainModel>() where TDomainModel : IDomainModelBase
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