// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            LogFormatter.Clearing += (o, e) => LogFormatter<EventLogInfo>.RegisterForAllMembers();
            LogFormatter<EventLogInfo>.RegisterForAllMembers();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogInfo"/> class.
        /// </summary>
        public EventLogInfo()
        {
            EntryType = EventLogEntryType.Information;
        }

        /// <summary>
        /// Gets or sets the type of the event log entry.
        /// </summary>
        public EventLogEntryType EntryType { get; set; }

        /// <summary>
        /// Gets or sets the event identifier.
        /// </summary>
        public int EventId { get; set; }
    }
}