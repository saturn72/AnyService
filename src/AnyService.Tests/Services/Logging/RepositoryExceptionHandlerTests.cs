using AnyService.Events;
using AnyService.Logging;
using AnyService.Services;
using AnyService.Services.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Logging
{
    public class RepositoryExceptionHandlerTests
    {
        [Fact]
        public async Task RuntimeExeptionHandler_HandlesException()
        {
            var wc = new WorkContext
            {
                CurrentUserId = "cur-user-id",
                CurrentClientId = "cur-client-id",
                IpAddress = "this is ip-address",
            };
            const string exceptionId = "exeption-id",
                exMsg = "msg-1",
                exRuntimeType = "ex-runtime-type",
                path = "this-is-path",
                method = "GET";

            var headers = new HeaderDictionary
            {
                { "key1", new StringValues("value1") },
                { "key2", new StringValues("value2") },
            };
            var queryString = new QueryString("?query-string-value");
            var host = new HostString("host", 123);

            var expRequest = new
            {
                url = host.Value,
                port = host.Port,
                method = method,
                path = path,
                headers = headers.Select(x => $"[{x.Key}:{x.Value}]").Aggregate((f, s) => $"{f}\n{s}"),
                query = queryString.Value,
            };

            var logRecord = new LogRecord
            {
                Level = LogRecordLevel.Error,
                ClientId = wc.CurrentClientId,
                UserId = wc.CurrentUserId,
                ExceptionId = exceptionId,
                ExceptionRuntimeType = exRuntimeType,
                ExceptionRuntimeMessage = exMsg,
                Message = "some-clevermessage",
                IpAddress = wc.IpAddress,
                RequestPath = path,
                RequestHeaders = expRequest.headers,
                HttpMethod = method,
                Request = expRequest.ToJsonString(),
                WorkContext = wc.ToJsonString()
            };
            var ded = new DomainEventData
            {
                Data = new
                {
                    exception = new Exception(),
                    logRecord = logRecord
                },
                PerformedByUserId = wc.CurrentUserId,
                WorkContext = wc,
            };

            var sp = new Mock<IServiceProvider>();
            var lm = new Mock<ILogRecordManager>();

            sp.Setup(p => p.GetService(typeof(ILogRecordManager))).Returns(lm.Object);

            var reh = new RepositoryExceptionHandler(sp.Object);
            await reh.InsertRecord(ded);
            lm.Verify(r => r.InsertLogRecord(It.Is<LogRecord>(lRec =>
                lRec.Level == LogRecordLevel.Error &&
                lRec.ClientId == wc.CurrentClientId &&
                lRec.UserId == wc.CurrentUserId &&
                lRec.ExceptionId == exceptionId &&
                lRec.ExceptionRuntimeType == exRuntimeType &&
                lRec.ExceptionRuntimeMessage == exMsg &&
                lRec.Message.HasValue() &&
                lRec.IpAddress == wc.IpAddress &&
                lRec.RequestPath == path &&
                lRec.RequestHeaders == expRequest.headers &&
                lRec.HttpMethod == method &&
                lRec.Request == expRequest.ToJsonString() &&
                lRec.WorkContext == wc.ToJsonString() &&
                lRec.CreatedOnUtc == default
            )), Times.Once);
        }
    }
}