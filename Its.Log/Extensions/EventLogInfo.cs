using System.Diagnostics;

namespace Its.Log.Instrumentation.Extensions
{
    /// <summary>
    /// Extends <see cref="LogEntry" /> with information to be added to the Windows event log.
    /// </summary>
    /// <remarks>If the <see cref="LogEntry" /> is not written to the event log by a subscriber, adding this extension has no effect.</remarks>
    public class EventLogInfo
    {
        static EventLogInfo()
        {
            Formatter.Clearing += (o, e) => Formatter<EventLogInfo>.RegisterForAllMembers();
            Formatter<EventLogInfo>.RegisterForAllMembers();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogInfo"/> class.
        /// </summary>
        public EventLogInfo()
        {
            EntryType = EventLogEntryType.Information;
        }

        public EventLogEntryType EntryType { get; set; }

        public int EventId { get; set; }
    }
}