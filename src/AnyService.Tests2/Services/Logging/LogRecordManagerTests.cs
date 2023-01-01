using AnyService.Logging;
using AnyService.Services;
using AnyService.Services.Logging;
using Microsoft.Extensions.Logging;

namespace AnyService.Tests.Services.Logging
{
    public class LogRecordManagerTests
    {
        #region Insert
        [Fact]
        public async Task LogRecordManager_Insert()
        {
            var repo = new Mock<IRepository<LogRecord>>();
            var log = new Mock<ILogger<LogRecordManager>>();
            var lm = new LogRecordManager(repo.Object, log.Object);
            var lr = new LogRecord
            {
                ClientId = "123"
            };

            await lm.InsertLogRecord(lr);

            repo.Verify(r => r.Insert(It.Is<LogRecord>(l => l == lr)), Times.Once);
        }
        #endregion
        #region Get All
        [Fact]
        public async Task GetAll_ReturnsEmptyArray_OnRepositoryNull()
        {
            var wc = new WorkContext
            {
                CurrentClientId = "cId",
                CurrentUserId = "uId",
            };

            var repo = new Mock<IRepository<LogRecord>>();
            repo.Setup(x => x.GetAll(It.IsAny<Pagination<LogRecord>>()))
                .ReturnsAsync(null as IEnumerable<LogRecord>);

            var logger = new Mock<ILogger<LogRecordManager>>();
            var lm = new LogRecordManager(repo.Object, logger.Object);
            var res = await lm.GetAll(new LogRecordPagination());
            var lp = res.ShouldBeOfType<LogRecordPagination>();
            lp.Data.ShouldBeNull();
        }
        [Fact]
        public async Task GetAll_ReturnsRepositoryData()
        {
            var wc = new WorkContext
            {
                CurrentClientId = "cId",
                CurrentUserId = "uId",
            };

            var repo = new Mock<IRepository<LogRecord>>();
            var repoData = new[]
            {
                new LogRecord { Id = "a" },
                new LogRecord { Id = "b" },
                new LogRecord { Id = "c" },
            };
            repo.Setup(x => x.GetAll(It.IsAny<Pagination<LogRecord>>())).ReturnsAsync(repoData);

            var logger = new Mock<ILogger<LogRecordManager>>();
            var lm = new LogRecordManager(repo.Object, logger.Object);
            var res = await lm.GetAll(new LogRecordPagination());
            var lp = res.ShouldBeOfType<LogRecordPagination>();
            lp.Data.ShouldBe(repoData);
        }
        #endregion
        #region Query Builder
        [Theory]
        [MemberData(nameof(BuildLogPaginationQuery_DATA))]
        public void BuildLogPaginationQuery(LogRecordPagination p, int[] selectedIndexes)
        {
            var a = new TestLogRecordManager();
            var q = a.QueryBuilder(p);

            var res = _records.Where(q).ToArray();
            res.Count().ShouldBe(selectedIndexes.Count());

            for (int i = 0; i < selectedIndexes.Length; i++)
                res.ShouldContain(x => x == _records.ElementAt(selectedIndexes[i]));
        }
        public static IEnumerable<object[]> BuildLogPaginationQuery_DATA => new[]
        {
            //ids
            new object[]
            {
                new LogRecordPagination
                {
                    LogRecordIds = new[]{"a", "b", "c"}
                },
                new[]{ 0,1,2}
            },
            //log levels
             new object[]
            {
                new LogRecordPagination
                {
                    LogLevels = new[]{LogRecordLevel.Information}
                },
                new[]{ 0,2}
            },
             //id + log levels
             new object[]
            {
                new LogRecordPagination
                {
                    LogRecordIds = new[]{"a", },
                    LogLevels = new[]{LogRecordLevel.Information}
                },
                new[]{ 0 }
            },
             //id + log levels
             new object[]
            {
                new LogRecordPagination
                {
                    LogRecordIds = new[]{"a", },
                    LogLevels = new[]{LogRecordLevel.Information}
                },
                new[]{ 0 }
            },
              //user id
             new object[]
            {
                new LogRecordPagination
                {
                    UserIds = new[]{"user-1", "user-2"},
                },
                new[]{ 1, 2, 3, 5 }
            },
               //user id + log Level
             new object[]
            {
                new LogRecordPagination
                {
                    UserIds = new[]{"user-1", "user-2"},
                    LogLevels = new[]{LogRecordLevel.Error}
                },
                new[]{ 3 }
            },
              //client id
             new object[]
            {
                new LogRecordPagination
                {
                    ClientIds = new[]{"client-1", "client-2"},
                },
                new[]{ 0, 2, 3, 4, 5 }
            },
               //client id + user id + log Level
             new object[]
            {
                new LogRecordPagination
                {
                    ClientIds = new[]{"client-1", "client-2"},
                    UserIds = new[]{"user-1", "user-2"},
                    LogLevels = new[]{LogRecordLevel.Information}
                },
                new[]{ 2 }
            },
              //exception id
             new object[]
            {
                new LogRecordPagination
                {
                    ExceptionIds = new[]{"1", "2", "3", "4"},
                },
                new[]{ 1, 2, 3, 4 }
            },
               //exception id + client id + user id + log Level
             new object[]
            {
                new LogRecordPagination
                {
                    ExceptionIds = new[]{"1", "2", "3", "4"},
                    UserIds = new[]{"user-1", "user-2"},
                    ClientIds = new[]{"client-1", "client-2"},
                    LogLevels = new[]{LogRecordLevel.Information, LogRecordLevel.Error }
                },
                new[]{ 2, 3 }
            },
             //exception runtime type 
             new object[]
            {
                new LogRecordPagination
                {
                    ExceptionRuntimeTypes = new[]{"int", "string"},
                },
                new[]{ 0, 1, 5 }
            },
             //runtime type + client id + level
             new object[]
            {
                new LogRecordPagination
                {
                    ExceptionRuntimeTypes = new[]{"int", "string"},
                    ClientIds = new[]{"client-1", "client-2"},
                    LogLevels = new[]{LogRecordLevel.Debug }
                },
                new[]{ 5 }
            },
             //ip addresses
             new object[]
            {
                new LogRecordPagination
                {
                    IpAddresses = new[]{"ip-0", "ip-1"},
                },
                new[]{ 0, 1, 2, 4 }
            },
             //ipaddress + runtime type + client id 
             new object[]
            {
                new LogRecordPagination
                {
                    IpAddresses = new[]{"ip-0", "ip-3"},
                    ExceptionRuntimeTypes = new[]{"int", "string"},
                    ClientIds = new[]{"client-1", "client-2"},
                },
                new[]{ 0 }
            },
             //method
             new object[]
            {
                new LogRecordPagination
                {
                    HttpMethods = new[]{"m-1", "m-2"},
                },
                new[]{ 0, 1, 2, 4 }
            },
             //method + ip address 
             new object[]
            {
                new LogRecordPagination
                {
                    HttpMethods = new[]{"m-1", "m-2"},
                    IpAddresses = new[]{"ip-0", "ip-3"},
                },
                new[]{ 0, }
            },
             //exception runtime message
             new object[]
            {
                new LogRecordPagination
                {
                    ExceptionRuntimeMessages = new[]{"ex-msg-1", "ex-msg-2"},
                },
                new[]{ 3, 4, 5 }
            },
             //exception runtime message + method 
             new object[]
            {
                new LogRecordPagination
                {
                    ExceptionRuntimeMessages = new[]{"ex-msg-1", "ex-msg-2"},
                    HttpMethods = new[]{"m-1", "m-3"},
                },
                new[]{ 5 }
            },
             //exception runtime message CONTAINS
             new object[]
            {
                new LogRecordPagination
                {
                    ExceptionRuntimeMessageContains = new[]{ "-1" },
                },
                new[]{ 3, 5 }
            },
             //exception runtime message CONTAINS + method 
             new object[]
            {
                new LogRecordPagination
                {
                    ExceptionRuntimeMessageContains = new[]{"-1", "-2"},
                    HttpMethods = new[]{"m-1", "m-3"},
                },
                new[]{ 5 }
            },
             //message
             new object[]
            {
                new LogRecordPagination
                {
                    Messages = new[]{ "msg-1" , "msg-2" },
                },
                new[]{ 2, 3 }
            },
             //exception runtime message CONTAINS + method 
             new object[]
            {
                new LogRecordPagination
                {
                    Messages= new[]{ "msg-2" , },
                    ExceptionRuntimeMessages= new[]{"ex-msg-1"},
                },
                new[]{ 3 }
            },
              //message contains
             new object[]
            {
                new LogRecordPagination
                {
                    MessageContains = new[]{ "msg-1" , "msg-2" },
                },
                new[]{ 2, 3, 4, 5 }
            },
             //exception runtime message CONTAINS + method 
             new object[]
            {
                new LogRecordPagination
                {
                    MessageContains= new[]{ "msg-1" , },
                    ExceptionRuntimeMessages= new[]{"ex-msg-1"},
                },
                new[]{ 5 }
            },
              //request path contains
             new object[]
            {
                new LogRecordPagination
                {
                    RequestPaths = new[]{ "rp-1" , "rp-2" },
                },
                new[]{ 0, 1, 2 }
            },
             //request path contains + client ids 
             new object[]
            {
                new LogRecordPagination
                {
                    RequestPaths = new[]{ "rp-1" , "rp-2" },
                    ClientIds = new[]{"client-1"}
                },
                new[]{ 2 }
            },
             //from
             new object[]
             {
                 new LogRecordPagination
                 {
                     FromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3))
                 },
                 new[]{ 3, 4, 5 }
             },
              //to
             new object[]
             {
                 new LogRecordPagination
                 {
                     ToUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(2))
                 },
                 new[]{ 0, 1, 2, 3, 4}
             },
             //from + to
              new object[]
             {
                 new LogRecordPagination
                 {
                     FromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3)),
                     ToUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(2))
                 },
                 new[]{ 3, 4 }
             },
        };

        public static readonly IEnumerable<LogRecord> _records = new[]
        {
            new LogRecord
            {
                Id = "a",
                Level = LogRecordLevel.Information,
                ClientId = "client-2",
                ExceptionRuntimeType = "int",
                IpAddress = "ip-0",
                HttpMethod = "m-1",
                RequestPath = "rp-1",
            },
            new LogRecord
            {
                Id = "b",
                UserId = "user-1",
                TraceId = "1",
                ExceptionRuntimeType = "int",
                IpAddress = "ip-1",
                HttpMethod = "m-1",
                RequestPath = "rp-2",
            },

            new LogRecord { Id = "c" ,
                Level = LogRecordLevel.Information,
                UserId = "user-1",
                ClientId = "client-1",
                TraceId = "2",
                IpAddress = "ip-1",
                HttpMethod = "m-1",
                Message = "msg-1",
                RequestPath = "rp-1",
            },

            new LogRecord
            {
                Id = "d" ,
                Level = LogRecordLevel.Error,
                UserId = "user-1",
                ClientId = "client-2",
                TraceId = "3",
                IpAddress = "ip-3",
                ExceptionRuntimeMessage = "ex-msg-1",
                Message = "msg-2",
                CreatedOnUtc= DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(3)).ToIso8601()
            },

            new LogRecord
            {
                Id = "e" ,
                Level = LogRecordLevel.Fatal,
                UserId = "user-3",
                ClientId = "client-1",
                TraceId = "4",
                IpAddress = "ip-1",
                HttpMethod = "m-2",
                ExceptionRuntimeMessage = "ex-msg-2",
                Message = "msg-11",
                CreatedOnUtc= DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)).ToIso8601()
            },

            new LogRecord
            {
                Id = "f" ,
                Level = LogRecordLevel.Debug,
                UserId = "user-2",
                ClientId = "client-2",
                ExceptionRuntimeType = "string",
                HttpMethod = "m-3",
                ExceptionRuntimeMessage = "ex-msg-1",
                Message = "msg-10",
                CreatedOnUtc= DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)).ToIso8601()
            },
        };
        public class TestLogRecordManager : LogRecordManager
        {
            public TestLogRecordManager() : base(null, null)
            {
            }
            public Func<LogRecord, bool> QueryBuilder(LogRecordPagination pagination) => BuildLogRecordPaginationQuery(pagination);
        }
        #endregion
    }
}
