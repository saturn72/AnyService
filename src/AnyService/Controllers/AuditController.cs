using AnyService.Audity;
using AnyService.Services;
using AnyService.Services.Audit;
using AnyService.Services.ServiceResponseMappers;
using AutoMapper.Configuration.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AnyService.Controllers
{
    [ApiController]
    [Route("audit")]
    public class AuditController<TDomainModel> : ControllerBase where TDomainModel : IDomainModelBase
    {
        #region fields
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditController<TDomainModel>> _logger;
        private readonly IServiceResponseMapper _serviceResponseMapper;
        private readonly Type _curType;
        private readonly string _curTypeName;
        #endregion

        #region ctor
        public AuditController(
            IAuditService auditService,
            ILogger<AuditController<TDomainModel>> logger,
            IServiceResponseMapper serviceResponseMapper
            )
        {
            _logger = logger;
            _serviceResponseMapper = serviceResponseMapper;
            _auditService = auditService;
            _curType = typeof(TDomainModel);
            _curTypeName = _curType.Name;
        }
        #endregion

        [HttpGet("{auditRecordType}")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int offset = 0,
            [FromQuery] int pageSize = 100,
            [FromQuery] string orderBy = null,
            [FromQuery] bool withNavProps = true,
            [FromQuery] string sortOrder = "desc",
            [FromQuery] string auditRecordType = "")
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Get all audit flow. With values: " +
                $"\'{nameof(auditRecordType)}\' = \'{auditRecordType}\', \'{nameof(orderBy)}\' = \'{orderBy}\', \'{nameof(offset)}\' = \'{offset}\', \'{nameof(pageSize)}\' = \'{pageSize}\', " +
                $"\'{nameof(withNavProps)}\' = \'{withNavProps}\', \'{nameof(sortOrder)}\' = \'{sortOrder}\'");

            var srvRes = new ServiceResponse();

            auditRecordType = auditRecordType.ToLower();
            if (!IsValidRequest(auditRecordType))
            {
                srvRes.Message = $"Unknown {nameof(AuditRecordTypes)}";
                srvRes.Result = ServiceResult.BadOrMissingData;
                return _serviceResponseMapper.Map(srvRes);
            }
            var pagination = GetAuditRecordPagination(offset, pageSize, orderBy, withNavProps, sortOrder);

            srvRes = await _auditService.GetAll(auditRecordType, pagination);
            _logger.LogDebug(LoggingEvents.Controller,
                $"Get all audit service result: '{srvRes.Result}', message: '{srvRes.Message}', exceptionId: '{srvRes.ExceptionId}', data: '{pagination.Data.ToJsonString()}'");
            srvRes.Data = pagination?.Map<PaginationModel<TDomainModel>>();
            return _serviceResponseMapper.Map(srvRes);
        }

        private bool IsValidRequest(string auditRecordType)
        {
            return
                (auditRecordType == AuditRecordTypes.CREATE && _curType is ICreatableAudit) ||
                (auditRecordType == AuditRecordTypes.READ && _curType is IReadableAudit) ||
                (auditRecordType == AuditRecordTypes.UPDATE && _curType is IUpdatableAudit) ||
                (auditRecordType == AuditRecordTypes.DELETE && _curType is IDeletableAudit);
        }

        private Pagination<TDomainModel> GetAuditRecordPagination(
            int offset,
            int pageSize,
            string orderBy,
            bool includeNested,
            string sortOrder
            )
        {
            return new Pagination<TDomainModel>
            {
                Offset = offset,
                OrderBy = orderBy,
                PageSize = pageSize,
                IncludeNested = includeNested,
                SortOrder = sortOrder
            };
        }
    }
}
