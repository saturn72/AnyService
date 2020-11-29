using AnyService.Audity;
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
    [ApiController]
    [Route("__audit")]
    public class AuditController : AnyServiceControllerBase
    {
        #region fields
        private static readonly KeyValuePair<string, string> DefaultKeyValuePair = default(KeyValuePair<string, string>);
        private static readonly IEnumerable<string> ValidAuditRecordTypes = new[]
        {
            AuditRecordTypes.CREATE,
            AuditRecordTypes.READ,
            AuditRecordTypes.UPDATE,
            AuditRecordTypes.DELETE,
        };

        private readonly IAuditManager _auditManager;
        private readonly AnyServiceConfig _config;
        private readonly WorkContext _workContext;
        private readonly IServiceResponseMapper _serviceResponseMapper;
        private readonly ILogger<AuditController> _logger;
        private static IReadOnlyDictionary<string, string> PropertiesProjectionMap;
        #endregion

        #region ctor
        public AuditController(
            IAuditManager auditManager,
            AnyServiceConfig config,
            WorkContext workContext,
            IServiceResponseMapper serviceResponseMapper,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            ILogger<AuditController> logger
            )
        {
            _auditManager = auditManager;
            _config = config;
            _workContext = workContext;
            _serviceResponseMapper = serviceResponseMapper;
            _logger = logger;
            PropertiesProjectionMap ??= entityConfigRecords.First(typeof(AuditRecord)).EndpointSettings.PropertiesProjectionMap;
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
            [FromQuery] string projectedFields = "",
            [FromQuery] int offset = 0,
            [FromQuery] int pageSize = 100,
            [FromQuery] string orderBy = null,
            [FromQuery] string sortOrder = PaginationSettings.Asc,
            [FromQuery] bool dataOnly = true
            )
        {
            _logger.LogInformation(LoggingEvents.Controller, $"Start Get all audit flow. With values: " +
                $"\'{nameof(auditRecordTypes)}\' = \'{auditRecordTypes}\'," +
                $"\'{nameof(entityNames)}\' = \'{entityNames}\'," +
                $"\'{nameof(entityIds)}\' = \'{entityIds}\'," +
                $"\'{nameof(userIds)}\' = \'{userIds}\'," +
                $"\'{nameof(clientIds)}\' = \'{clientIds}\'," +
                $"\'{nameof(fromUtc)}\' = \'{fromUtc}\'," +
                $"\'{nameof(toUtc)}\' = \'{toUtc}\'," +
                $" \'{nameof(projectedFields)}\' = \'{projectedFields}\', " +
                $" \'{nameof(orderBy)}\' = \'{orderBy}\', " +
                $"\'{nameof(offset)}\' = \'{offset}\', " +
                $"\'{nameof(pageSize)}\' = \'{pageSize}\', " +
                $"\'{nameof(sortOrder)}\' = \'{sortOrder}\'");

            var toBeProjected = projectedFields?.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

            var pagination = QueryParamsToPagination(
                auditRecordTypes, entityNames,
                entityIds, userIds,
                clientIds,
                fromUtc, toUtc,
                toBeProjected,
                offset, pageSize,
                orderBy, sortOrder);

            if (!IsValidRequest(pagination))
                return BadRequest();

            var srvRes = await _auditManager.GetAll(pagination);
            _logger.LogDebug(LoggingEvents.Controller,
                $"Get all audit service result: '{srvRes.Result}', message: '{srvRes.Message}', {nameof(ServiceResponse.TraceId)}: '{srvRes.TraceId}', data: '{pagination.Data.ToJsonString()}'");

            return ToPaginationActionResult(srvRes, dataOnly, toBeProjected);
        }
        private IActionResult ToPaginationActionResult(ServiceResponse<AuditPagination> serviceResponse, bool dataOnly, IEnumerable<string> toBeProjected)
        {
            toBeProjected = toBeProjected?.Select(s => char.ToLowerInvariant(s[0]) + s[1..]); //make camelCase
            if (dataOnly && serviceResponse.ValidateServiceResponse())
            {
                var d = serviceResponse.Payload.Data.Map<IEnumerable<AuditRecordModel>>(_config.MapperName);
                return toBeProjected.IsNullOrEmpty() ?
                        JsonResult(d) :
                        JsonResult(d.Select(x => x.ToDynamic(toBeProjected)).ToArray());
            }

            if (toBeProjected.IsNullOrEmpty())
                return _serviceResponseMapper.MapServiceResponse<AuditPaginationModel>(serviceResponse);

            var projSrvRes = new ServiceResponse(serviceResponse)
            {
                PayloadObject = serviceResponse.Payload.Data.Select(x => x.ToDynamic(toBeProjected)).ToArray()
            };
            return _serviceResponseMapper.MapServiceResponse<AuditPaginationModel>(serviceResponse);
        }

        private AuditPagination QueryParamsToPagination(
            string auditRecordTypes,
            string entityNames,
            string entityIds,
            string userIds,
            string clientIds,
            string fromUtc,
            string toUtc,
            IEnumerable<string> projectedFields,
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
            var toProject = projectedFields.IsNullOrEmpty() ? new string[] { } : getProjectionMap();
            if (toProject == null) return null;

            return new AuditPagination
            {
                AuditRecordTypes = splitOrNull(auditRecordTypes),
                EntityNames = splitOrNull(entityNames),
                EntityIds = splitOrNull(entityIds),
                UserIds = getUserIdsOrNull(userIds),
                ClientIds = getClientIdsOrNull(clientIds),
                FromUtc = fromDate,
                ToUtc = toDate,
                ProjectedFields = toProject,
                Offset = offset,
                PageSize = pageSize,
                OrderBy = orderBy,
                SortOrder = sortOrder
            };

            static IEnumerable<string> splitOrNull(string source) =>
                source?.Split(",", StringSplitOptions.RemoveEmptyEntries);

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
            IEnumerable<string> getProjectionMap()
            {
                var r = new List<string>();

                foreach (var pf in projectedFields)
                {
                    var t = PropertiesProjectionMap.FirstOrDefault(a => a.Value.Equals(pf, StringComparison.InvariantCultureIgnoreCase));
                    if (t.Equals(DefaultKeyValuePair))
                        return null;
                    r.Add(t.Key);
                }
                return r;
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
}