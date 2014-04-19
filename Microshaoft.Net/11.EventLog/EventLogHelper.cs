namespace ConsoleApplication
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Collections.Generic;
    using Microshaoft;
    public class Program
    {
        static void Main(string[] args)
        {
            //
            // TODO: 在此处添加代码以启动应用程序
            //
            var log = "TestLog001";
            var source = "TestSource001";
            var eventLogs = EventLog.GetEventLogs();
            Array.ForEach
                    (
                        eventLogs
                        , (x) =>
                        {
                            Console.WriteLine
                                        (
                                            "Source: {1}{0}Log: {2}"
                                            , "\t"
                                            , x.Source
                                            , x.Log
                                        );
                        }
                    );
            EventLogHelper.TryCreateEventLogSource(log, source);
            EventLogHelper.WriteEventLogEntry
                            (
                                source
                                , "TestMessage"
                                , EventLogEntryType.Information
                                , 1000
                            );
            Console.WriteLine("Hello World");
            Console.WriteLine(Environment.Version.ToString());
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;
    public static class EventLogHelper
    {
        public static EventLog[] GetEventLogs()
        {
            var r = EventLog.GetEventLogs();
            return r;
        }

        public static void WriteEventLogEntry
                                        (
                                            //string logName,
                                            string sourceName,
                                            string logMessage,
                                            EventLogEntryType logEntryType
                                            , int eventID
                                        )
        {
            EventLog eventLog = new EventLog();
            eventLog.Source = sourceName;
            //eventLog.Log = logName;
            eventLog.WriteEntry(logMessage, logEntryType, eventID);
        }
        public static bool TryCreateEventLogSource
                                        (
                                            string logName,
                                            string sourceName,
                                            bool delete = false
                                        )
        {
            bool r = false;

            if (EventLog.SourceExists(sourceName))
            {
                if (delete)
                {
                    try
                    {
                        var s = EventLog.LogNameFromSourceName(sourceName, ".");
                        if (string.Compare(s, logName, true) == 0)
                        {
                            EventLog.DeleteEventSource(sourceName);
                            EventLog.Delete(logName);
                            EventLog.CreateEventSource(sourceName, logName);
                            r = true;
                        }
                    }
                    catch// (Exception e)
                    {
                        r = false;
                    }
                }
                else
                {
                    r = true;
                }
                
            }
            else
            {
                EventLog.CreateEventSource(sourceName, logName);
                r = true;
            }
            return r;
        }
    }
}
