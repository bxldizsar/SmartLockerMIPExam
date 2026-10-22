using System;
using System.Diagnostics;

namespace SmartLocker.Services
{
    // writes events to trace.log, NEVER the pickup code
    public static class TraceLogger
    {
        private static bool initialized = false;

        public static void Initialize(string logFilePath)
        {
            if (initialized)
            {
                return;
            }

            // the listener appends to the file, so old runs stay in it
            Trace.Listeners.Add(new TextWriterTraceListener(logFilePath, "SmartLockerLog"));
            Trace.AutoFlush = true;
            initialized = true;

            Trace.WriteLine(Timestamp() + " | APP_START");
        }

        public static void LogStored(int parcelId, int compartmentId)
        {
            Trace.WriteLine(Timestamp() + " | STORED | ParcelId=" + parcelId + " | CompartmentId=" + compartmentId);
            Trace.Flush();
        }

        public static void LogCollected(int parcelId, int compartmentId)
        {
            Trace.WriteLine(Timestamp() + " | COLLECTED | ParcelId=" + parcelId + " | CompartmentId=" + compartmentId);
            Trace.Flush();
        }

        public static void Close()
        {
            Trace.Flush();
            Trace.Close();
        }

        private static string Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
