using System;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Carries instrumentation information.
    /// </summary>
    public class InstrumentationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstrumentationEventArgs"/> class.
        /// </summary>
        /// <param name="logEntry">The log entry.</param>
        internal InstrumentationEventArgs(LogEntry logEntry)
        {
            LogEntry = logEntry;
        }

        /// <summary>
        /// Gets the payload.
        /// </summary>
        /// <value>The payload.</value>
        public LogEntry LogEntry { get; private set; }
    }
}