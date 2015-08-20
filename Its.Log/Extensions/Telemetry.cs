using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Its.Log.Instrumentation.Extensions
{
    public sealed class Telemetry : IApplyOnExit
    {
        private IDictionary<string, object> properties;

        static Telemetry()
        {
            Formatter<Telemetry>.RegisterForAllMembers();
        }

        public bool Succeeded { get; set; }

        public string OperationName { get; set; }

        public int ElapsedMilliseconds { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public Uri RequestUri { get; set; }

        public IDictionary<string, object> Properties
        {
            get
            {
                return properties ?? (properties = new Dictionary<string, object>());
            }
        }

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