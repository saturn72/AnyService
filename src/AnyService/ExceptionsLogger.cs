using System;
using System.Collections.Generic;
using System.Diagnostics;
using AnyService.Utilities;
using Microsoft.Extensions.Logging;

public class ExceptionsLogger
{
    internal static bool WasInit;
    private static readonly object lockObj = new object();
    private static IDictionary<string, int> _eventIndexes = new Dictionary<string, int>();
    private static ILogger Logger;
    private static IIdGenerator IdGenerator;
    public static void Init(ILogger logger, IIdGenerator idGenerator)
    {
        Logger = logger;
        IdGenerator = idGenerator;
        WasInit = true;
    }

    public static string Log(Exception exception)
    {
        var exId = IdGenerator.GetNext<string>();
        var callerName = new StackTrace().GetFrame(0).GetMethod().Name;
        var eventIndex = GetEventIndex(callerName);
        var eventId = new EventId(eventIndex);
        Logger.LogError(eventId, exception, $"{exId} : {callerName}");
        return exId;
    }
    private static int GetEventIndex(string callerName)
    {
        lock (lockObj)
        {
            if (!_eventIndexes.TryGetValue(callerName, out int value))
            {
                value = _eventIndexes.Count + 1;
                _eventIndexes[callerName] = value;
            }
            return value;
        }
    }
}