// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Monitor trace and debug output and writes it to <see cref="Log.Write(LogEntry)"/>.
    /// </summary>
    [DebuggerStepThrough]
    public class TraceListener : System.Diagnostics.TraceListener
    {
        internal static readonly RecursionCounter RecursionCounter = new RecursionCounter();

        /// <summary>
        /// Writes trace input to <see cref="Log.Write(LogEntry)" />.
        /// </summary>
        /// <remarks>This is the method into which all TraceListener Write methods call.</remarks>
        private static void LogWrite(object o, string category = null)
        {
            if (RecursionCounter.Depth > 0)
            {
                return;
            }
            using (RecursionCounter.Enter())
            {
                LogEntry logEntry;

                var d = o as Delegate;
                if (d != null)
                {
                    logEntry = new LogEntry(d.DynamicInvoke(), d.GetAnonymousMethodInfo());
                }
                else if (o is LogEntry)
                {
                    logEntry = (LogEntry) o;
                }
                else
                {
                    logEntry = new LogEntry(o);
                }

                logEntry.Category = category;
                Log.Write(logEntry);
            }
        }

        /// <summary>
        /// Writes trace information, a message, a related activity identity and event information to the listener specific output.
        /// </summary>
        /// <param name="eventCache">A <see cref="T:System.Diagnostics.TraceEventCache"/> object that contains the current process ID, thread ID, and stack trace information.</param>
        /// <param name="source">A name used to identify the output, typically the name of the application that generated the trace event.</param>
        /// <param name="id">A numeric identifier for the event.</param>
        /// <param name="message">A message to write.</param>
        /// <param name="relatedActivityId">A <see cref="T:System.Guid"/>  object identifying a related activity.</param>
        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            // TODO: (TraceTransfer) use extensibility: entry.RelatedActivityId = relatedActivityId;entry.Source = source;
            LogWrite(message);
        }

        /// <summary>
        /// Writes a category name and a message to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class.
        /// </summary>
        /// <param name="message">A message to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        public override void Write(string message, string category)
        {
            LogWrite(message, category);
        }

        /// <summary>
        /// Writes a category name and the value of the object's <see cref="M:System.Object.ToString" /> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object" /> whose fully qualified class name you want to write. </param>
        /// <param name="category">A category name used to organize the output. </param><filterpriority>2</filterpriority>
        public override void Write(object o, string category)
        {
            LogWrite(o, category);
        }

        /// <summary>
        /// Writes the value of the object's <see cref="M:System.Object.ToString"/> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object"/> whose fully qualified class name you want to write.</param>
        public override void Write(object o)
        {
            LogWrite(o);
        }

        /// <summary>
        /// When overridden in a derived class, writes the specified message to the listener you create in the derived class.
        /// </summary>
        /// <param name="message">A message to write.</param>
        public override void Write(string message)
        {
            LogWrite(message);
        }

        /// <summary>
        /// Writes a category name and a message to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class, followed by a line terminator.
        /// </summary>
        /// <param name="message">A message to write. </param>
        /// <param name="category">A category name used to organize the output. </param><filterpriority>2</filterpriority>
        public override void WriteLine(string message, string category)
        {
            LogWrite(message, category);
        }

        /// <summary>
        /// Writes a category name and the value of the object's <see cref="M:System.Object.ToString" /> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class, followed by a line terminator.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object" /> whose fully qualified class name you want to write. </param>
        /// <param name="category">A category name used to organize the output. </param><filterpriority>2</filterpriority>
        public override void WriteLine(object o, string category)
        {
            LogWrite(o, category);
        }

        /// <summary>
        /// When overridden in a derived class, writes a message to the listener you create in the derived class, followed by a line terminator.
        /// </summary>
        /// <param name="message">A message to write. </param><filterpriority>2</filterpriority>
        public override void WriteLine(string message)
        {
            LogWrite(message);
        }

        /// <summary>
        /// Writes the value of the object's <see cref="M:System.Object.ToString" /> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class, followed by a line terminator.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object" /> whose fully qualified class name you want to write. </param><filterpriority>2</filterpriority>
        public override void WriteLine(object o)
        {
            LogWrite(o);
        }
    }
}