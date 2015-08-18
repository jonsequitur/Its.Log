using System;
using System.Linq;
using System.Net;

namespace Its.Log.Instrumentation.Extensions
{
    public class Telemetry : IApplyOnExit
    {
        static Telemetry()
        {
            Formatter<Telemetry>.RegisterForAllMembers();
        }

        public bool Succeeded { get; set; }

        public string OperationName { get; set; }

        public int ElapsedMilliseconds { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public Uri RequestUri { get; set; }

        public void OnExit(LogEntry logEntry)
        {
            Succeeded = logEntry.Confirmations
                                .OfType<Telemetry>()
                                .Any();
            OperationName = OperationName ?? logEntry.CallingMethod;
            ElapsedMilliseconds = (int) logEntry.ElapsedMilliseconds;
        }
    }
}