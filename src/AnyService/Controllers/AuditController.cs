using AnyService;
using AnyService.Models;
using AnyService.Services;
using AnyService.Services.Audit;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Controllers
{
}
[ApiController]
[Route("__audit")]
public class AuditController : ControllerBase
{
    #region fields
    private static readonly IEnumerable<string> ValidAuditRecordTypes = new[]
    {
            AuditRecordTypes.CREATE,
            AuditRecordTypes.READ,
            AuditRecordTypes.UPDATE,
            AuditRecordTypes.DELETE,
        };

    private readonly IAuditManager _auditManager;
    private readonly ILogger<AuditController> _logger;
    private readonly WorkContext _workContext;
    private readonly IServiceResponseMapper _serviceResponseMapper;
    #endregion

    #region ctor
    public AuditController(
        IAuditManager auditManager,
        ILogger<AuditController> logger,
        WorkContext workContext,
        IServiceResponseMapper serviceResponseMapper
        )
    {
        _auditManager = auditManager;
        _logger = logger;
        _workContext = workContext;
        _serviceResponseMapper = serviceResponseMapper;
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
        _logger.LogDebug(LoggingEvents.Controller, $"Start Get all audit flow. With values: " +
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
            return _serviceResponseMapper.MapServiceResponse<AuditPagination, AuditPaginationModel>(srvRes);
        
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
            UserIds = getUserIdsOrNull(userIds),
            ClientIds = getClientIdsOrNull(clientIds),
            FromUtc = fromDate,
            ToUtc = toDate,
            Offset = offset,
            PageSize = pageSize,
            OrderBy = orderBy,
            SortOrder = sortOrder
        };

        static IEnumerable<string> splitOrNull(string source)
        {
            return source.HasValue() ?
                source.Split(",", StringSplitOptions.RemoveEmptyEntries)
                : null;
        }

        IEnumerable<string> getUserIdsOrNull(string userIds)
        {
            //if (_workContext.IsInRole(_auditConfig.Role) )return splitOrNull(userIds);==> this is for admins
            return _workContext.CurrentUserId.HasValue() ? new[] { _workContext.CurrentUserId } : null;
        }
        IEnumerable<string> getClientIdsOrNull(string clientIds)
        {
            //if (_workContext.IsInRole(_auditConfig.Role) )return splitOrNull(clientIds);==> this is for admins
            return _workContext.CurrentClientId.HasValue() ? new[] { _workContext.CurrentClientId } : null;
        }
        static DateTime? getDateTimeOrNull(string iso8601)
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
            foreach (var art in pagination.AuditRecordTypes)
                if (!ValidAuditRecordTypes.Contains(art))
                    return false;
        return true;
    }
}