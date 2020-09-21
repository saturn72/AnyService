using AnyService.Logging;
using AnyService.Services;
using AnyService.Services.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Controllers
{
    [ApiController]
    [Route("__log")]
    [Authorize(Roles = "log-record-read")]
    public class LogRecordController : ControllerBase
    {
        #region fields
        private static readonly IEnumerable<string> ValidLogLevels = new[]
        {
            LogRecordLevel.Debug,
             LogRecordLevel.Information,
            LogRecordLevel.Warning,
            LogRecordLevel.Error,
            LogRecordLevel.Fatal,
        };

        private readonly ILogRecordManager _logManager;
        private readonly ILogger<LogRecordController> _logger;
        #endregion

        #region ctor
        public LogRecordController(
            ILogRecordManager auditManager,
            ILogger<LogRecordController> logger
            )
        {
            _logManager = auditManager;
            _logger = logger;
        }
        #endregion

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string logRecordIds = "",
            [FromQuery] string logLevels = "",
            [FromQuery] string userIds = "",
            [FromQuery] string clientIds = "",
            [FromQuery] string exceptionIds = "",
            [FromQuery] string exceptionRuntimeTypes = "",
            [FromQuery] string ipAddresses = "",
            [FromQuery] string httpMethods = "",
            [FromQuery] string exceptionRuntimeMessages = "",
            [FromQuery] string exceptionRuntimeMessageContains = "",
            [FromQuery] string messages = "",
            [FromQuery] string messageContains = "",
            [FromQuery] string requestPaths = "",
            [FromQuery] string requestPathContains = "",
            [FromQuery] string fromUtc = "",
            [FromQuery] string toUtc = "",
            [FromQuery] int offset = 0,
            [FromQuery] int pageSize = 100,
            [FromQuery] string orderBy = null,
            [FromQuery] string sortOrder = "desc"
            )
        {
            _logger.LogDebug(LoggingEvents.Controller, $"Start Get all log-records flow. With values: " +
                $"\'{nameof(logRecordIds)}\' = \'{logRecordIds}\'," +
                $"\'{nameof(logLevels)}\' = \'{logLevels}\'," +
                $"\'{nameof(userIds)}\' = \'{userIds}\'," +
                $"\'{nameof(clientIds)}\' = \'{clientIds}\'," +
                $"\'{nameof(exceptionIds)}\' = \'{exceptionIds}\'," +
                $"\'{nameof(exceptionRuntimeTypes)}\' = \'{exceptionRuntimeTypes}\'," +
                $"\'{nameof(ipAddresses)}\' = \'{ipAddresses}\'," +
                $"\'{nameof(httpMethods)}\' = \'{httpMethods}\'," +
                $"\'{nameof(exceptionRuntimeMessages)}\' = \'{exceptionRuntimeMessages}\'," +
                $"\'{nameof(exceptionRuntimeMessageContains)}\' = \'{exceptionRuntimeMessageContains}\'," +
                $"\'{nameof(messages)}\' = \'{messages}\'," +
                $"\'{nameof(messageContains)}\' = \'{messageContains}\'," +
                $"\'{nameof(requestPaths)}\' = \'{requestPaths}\'," +
                $"\'{nameof(requestPathContains)}\' = \'{requestPathContains}\'," +
                $"\'{nameof(fromUtc)}\' = \'{fromUtc}\'," +
                $"\'{nameof(toUtc)}\' = \'{toUtc}\'," +
                $" \'{nameof(orderBy)}\' = \'{orderBy}\', " +
                $"\'{nameof(offset)}\' = \'{offset}\', " +
                $"\'{nameof(pageSize)}\' = \'{pageSize}\', " +
                $"\'{nameof(sortOrder)}\' = \'{sortOrder}\'");

            var pagination = QueryParamsToPagination(
                logRecordIds, logLevels,
                userIds, clientIds,
                exceptionIds, exceptionRuntimeTypes,
                ipAddresses, httpMethods,
                exceptionRuntimeMessages, exceptionRuntimeMessageContains,
                messages, messageContains,
                requestPaths, requestPathContains,
                fromUtc, toUtc,
                offset, pageSize,
                orderBy, sortOrder);

            if (!IsValidRequest(pagination))
                return BadRequest();

            var page = await _logManager.GetAll(pagination);
            _logger.LogDebug(LoggingEvents.Controller,
                $"Get all log-records result: '{page}', data: '{pagination.Data.ToJsonString()}'");
            return Ok(page);
        }

        private LogRecordPagination QueryParamsToPagination(
            string logRecordIds,
            string logLevels,
            string userIds,
            string clientIds,
            string exceptionIds,
            string exceptionRuntimeTypes,
            string ipAddresses,
            string httpMethods,
            string exceptionRuntimeMessages,
            string exceptionRuntimeMessageContains,
            string messages,
            string messageContains,
            string requestPaths,
            string requestPathContains,
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

            return new LogRecordPagination
            {
                LogRecordIds = splitOrNull(logRecordIds),
                LogLevels = splitOrNull(logLevels),
                UserIds = splitOrNull(userIds),
                ClientIds = splitOrNull(clientIds),
                ExceptionIds = splitOrNull(exceptionIds),
                ExceptionRuntimeTypes = splitOrNull(exceptionRuntimeTypes),
                IpAddresses = splitOrNull(ipAddresses),
                HttpMethods = splitOrNull(httpMethods),
                ExceptionRuntimeMessages = splitOrNull(exceptionRuntimeMessages),
                ExceptionRuntimeMessageContains = splitOrNull(exceptionRuntimeMessageContains),
                Messages = splitOrNull(messages),
                MessageContains = splitOrNull(messageContains),
                RequestPaths = splitOrNull(requestPaths),
                RequestPathContains = splitOrNull(requestPathContains),
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
            static DateTime? getDateTimeOrNull(string iso8601)
            {
                if (DateTime.TryParse(iso8601, out DateTime value))
                    return value;
                return null;
            }
        }

        private bool IsValidRequest(LogRecordPagination pagination)
        {
            if (pagination == null) return false;

            if (!pagination.LogLevels.IsNullOrEmpty())
                foreach (var art in pagination.LogLevels)
                    if (!ValidLogLevels.Contains(art))
                        return false;
            return true;
        }
    }
}
