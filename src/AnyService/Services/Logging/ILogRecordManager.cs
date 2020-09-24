using AnyService.Logging;
using System.Threading.Tasks;

namespace AnyService.Services.Logging
{
    public interface ILogRecordManager
    {
        Task<LogRecord> InsertLogRecord(LogRecord logRecord);
        Task<LogRecordPagination> GetAll(LogRecordPagination pagination);
    }
}
