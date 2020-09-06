using AnyService.Audity;
using AnyService.Services;
using AnyService.Services.Audit;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Controllers
{
    [ApiController]
    [Route("audit")]
    public class AuditController<TDomainModel> : ControllerBase where TDomainModel : IDomainModelBase
    {
        #region fields
        private readonly IAuditManager _auditManager;
        private readonly ILogger<AuditController<TDomainModel>> _logger;
        private readonly IServiceResponseMapper _serviceResponseMapper;
        private readonly Type _curType;
        private readonly string _curTypeName;
        #endregion

        #region ctor
        public AuditController(
            IAuditManager auditService,
            ILogger<AuditController<TDomainModel>> logger,
            IServiceResponseMapper serviceResponseMapper
            )
        {
            _logger = logger;
            _serviceResponseMapper = serviceResponseMapper;
            _auditManager = auditService;
            _curType = typeof(TDomainModel);
            _curTypeName = _curType.Name;
        }
        #endregion

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string auditRecordTypes = "",
            [FromQuery] string entityNames = "",
            [FromQuery] string entityIds = "",
            [FromQuery] string userIds = "",
            [FromQuery] string clientIds = "",
            [FromQuery] string fromUtc = "",
            [FromQuery] string toUtc = "",
            [FromQuery] int offset = 0,
            [FromQuery] int pageSize = 100,
            [FromQuery] string orderBy = null,
            [FromQuery] string sortOrder = "desc"
            )
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Get all audit flow. With values: " +
                $"\'{nameof(auditRecordTypes)}\' = \'{auditRecordTypes}\'," +
                $"\'{nameof(entityNames)}\' = \'{entityNames}\'," +
                $"\'{nameof(entityIds)}\' = \'{entityIds}\'," +
                $"\'{nameof(userIds)}\' = \'{userIds}\'," +
                $"\'{nameof(clientIds)}\' = \'{clientIds}\'," +
                $"\'{nameof(fromUtc)}\' = \'{fromUtc}\'," +
                $"\'{nameof(toUtc)}\' = \'{toUtc}\'," +
                $" \'{nameof(orderBy)}\' = \'{orderBy}\', " +
                $"\'{nameof(offset)}\' = \'{offset}\', " +
                $"\'{nameof(pageSize)}\' = \'{pageSize}\', " +
                $"\'{nameof(sortOrder)}\' = \'{sortOrder}\'");

            var pagination = QueryParamsToPagination(
                auditRecordTypes, entityNames, entityIds, userIds, clientIds,
                fromUtc, toUtc, offset, pageSize, orderBy, sortOrder);

            if (!IsValidRequest(pagination))
                return BadRequest();


            var srvRes = await _auditManager.GetAll(pagination);
            _logger.LogDebug(LoggingEvents.Controller,
                $"Get all audit service result: '{srvRes.Result}', message: '{srvRes.Message}', exceptionId: '{srvRes.ExceptionId}', data: '{pagination.Data.ToJsonString()}'");
            srvRes.Data = pagination?.Map<PaginationModel<AuditRecord>>();
            return _serviceResponseMapper.Map(srvRes);
        }

        private AuditPagination QueryParamsToPagination(
            string auditRecordTypes,
            string entityNames,
            string entityIds,
            string userIds,
            string clientIds,
            string fromUtc,
            string toUtc,
            int offset,
            int pageSize,
            string orderBy,
            string sortOrder)
        {
            var fromDate = getDateTimeOrNull(fromUtc);
            var toDate = getDateTimeOrNull(toUtc);
            if (
                (fromUtc.HasValue() && fromDate == null) ||
                (toUtc.HasValue() && toDate == null))
                return null;

            return new AuditPagination
            {
                AuditRecordTypes = splitOrNull(auditRecordTypes),
                EntityNames = splitOrNull(entityNames),
                EntityIds = splitOrNull(entityIds),
                UserIds = splitOrNull(userIds),
                ClientIds = splitOrNull(clientIds),
                FromUtc = fromDate,
                ToUtc = toDate,
                Offset = offset,
                PageSize = pageSize,
                OrderBy = orderBy,
                SortOrder = sortOrder
            };

            IEnumerable<string> splitOrNull(string source)
            {
                return source.HasValue() ?
                    source.Split(",", StringSplitOptions.RemoveEmptyEntries)
                    : null;
            }
            DateTime? getDateTimeOrNull(string iso8601)
            {
                if (DateTime.TryParse(iso8601, out DateTime value))
                    return value;
                return null;
            }
        }

        private bool IsValidRequest(AuditPagination pagination)
        {
            if (pagination == null) return false;

            if (!pagination.AuditRecordTypes.IsNullOrEmpty())
            {
                foreach (var art in pagination.AuditRecordTypes)
                    switch (art)
                    {
                        case AuditRecordTypes.CREATE:
                            if (_curType is ICreatableAudit) break;
                            return false;
                        case AuditRecordTypes.READ:
                            if (_curType is IReadableAudit) break;
                            return false;
                        case AuditRecordTypes.UPDATE:
                            if (_curType is IUpdatableAudit) break;
                            return false;
                        case AuditRecordTypes.DELETE:
                            if (_curType is IDeletableAudit) break;
                            return false;

                        default: return false;
                    }
            }

            return true;
        }

        private AuditPagination GetAuditRecordPagination(
            int offset,
            int pageSize,
            string orderBy,
            string sortOrder
            )
        {
            return new AuditPagination
            {
                Offset = offset,
                OrderBy = orderBy,
                PageSize = pageSize,
                SortOrder = sortOrder
            };
        }
    }
}
