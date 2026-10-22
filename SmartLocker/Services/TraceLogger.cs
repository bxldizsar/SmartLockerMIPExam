using System;
using System.Diagnostics;

namespace SmartLocker.Services
{
    // writes events into trace.log, never the pickup code
    public static class TraceLogger
    {
        private static bool initialized = false;

        // called once at startup from Program.cs
        public static void Initialize(string logFilePath)
        {
            if (initialized)
            {
                return;
            }

            // from now on every Trace.WriteLine also goes into the file
            // the file is not cleared, new lines are added at the end
            Trace.Listeners.Add(new TextWriterTraceListener(logFilePath, "SmartLockerLog"));
            Trace.AutoFlush = true;
            initialized = true;

            Trace.WriteLine(Timestamp() + " | APP_START");
        }

        // event 1: parcel put into a box
        public static void LogStored(int parcelId, int compartmentId)
        {
            Trace.WriteLine(Timestamp() + " | STORED | ParcelId=" + parcelId + " | CompartmentId=" + compartmentId);
            Trace.Flush();
        }

        // event 2: parcel picked up
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