// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Its.Log.Instrumentation.Extensions;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// A series of associated events.
    /// </summary>
    public class LogActivity : ILogActivity
    {
        private readonly LogEntry entry;
        private List<Delegate> paramsAccessors;
        private int isCompletedFlag;
        private bool requireConfirm = false;
        private readonly Action<LogEntry> onComplete;
        private readonly Queue<LogEntry> buffer;
        private readonly Lazy<ConfirmationList> confirmations = new Lazy<ConfirmationList>(() => new ConfirmationList());
        private Stopwatch stopwatch = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogActivity" /> class.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="requireConfirm">if set to <c>true</c>, log entries are buffered in memory and will not be emitted to subscripbers unless <see cref="Confirm" /> is called.</param>
        /// <param name="onComplete">The on complete.</param>
        public LogActivity(
            LogEntry entry,
            bool requireConfirm = false,
            Action<LogEntry> onComplete = null)
        {
            entry.EventType = TraceEventType.Start;
            this.entry = entry;

            // TODO: (LogActivity) generalize extensions that are triggered on boundary enter
            if (Extension<Counter>.IsEnabledFor(entry.CallingType))
            {
                Counter.For(entry.CallingType, entry.CallingMethod).Increment();
            }

            this.requireConfirm = requireConfirm;
            this.onComplete = onComplete;

            if (requireConfirm)
            {
                buffer = new Queue<LogEntry>();
            }

            Write(entry);
            StartTiming();
        }

        /// <summary>
        /// Completes the current activity.
        /// </summary>
        public void Complete(Action magicBarbell)
        {
            if (Interlocked.CompareExchange(ref isCompletedFlag, 1, 0) != 0)
            {
                return;
            }

            if (magicBarbell != null)
            {
                entry.Message = null;
                entry.AnonymousMethodInfo = magicBarbell.GetAnonymousMethodInfo();
            }

            var clone = entry.Clone(false);
            clone.EventType = TraceEventType.Stop;

            stopwatch?.Stop();

            if (confirmations.IsValueCreated)
            {
                Log.WithParams(() => new { Confirmed = confirmations.Value })
                   .ApplyTo(clone);
                clone.Confirmations = confirmations.Value.Select(c => c.ResolvedValue);
            }

            onComplete?.Invoke(clone);

            Write(clone);
        }

        /// <summary>
        /// Writes out buffered entries in a log activity that was entered with requireConfirm set to true.
        /// </summary>
        public void Confirm(Func<object> value = null)
        {
            value = value ?? (() => true);

            long? elapsedMilliseconds = null;

            if (stopwatch != null)
            {
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            confirmations.Value.Add(new Confirmation(value, elapsedMilliseconds));

            if (requireConfirm)
            {
                requireConfirm = false;
                while (buffer.Count > 0)
                {
                    Log.Write(buffer.Dequeue());
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Complete(null);

        /// <summary>
        /// Gets a value indicating whether this activity is completed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is completed; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompleted => isCompletedFlag == 1;

        /// <summary>
        /// Writes a log message associated with the current activity.
        /// </summary>
        public void Trace(string comment)
        {
            if (IsCompleted)
            {
                return;
            }

            var clone = entry.Clone(false);
            clone.EventType = TraceEventType.Verbose;
            clone.Message = comment;
            if (paramsAccessors != null)
            {
                foreach (var accessor in paramsAccessors)
                {
                    entry.AddInfo("Traced", accessor.DynamicInvoke());
                }
            }
            Write(clone);
        }

        /// <summary>
        /// Logs variables associated with the current activity.
        /// </summary>
        public void Trace<T>(Func<T> paramsAccessor) where T : class => TraceInner(paramsAccessor, false);

        /// <summary>
        /// Logs variables associated with the current activity and registers them to be logged again each time additional events are logged in the activity, so that their updated values will be recorded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paramsAccessor"></param>
        public void TraceAndWatch<T>(Func<T> paramsAccessor) where T : class
        {
            if (paramsAccessors == null)
            {
                paramsAccessors = new List<Delegate>();
            }
            paramsAccessors.Add(paramsAccessor);

            TraceInner(paramsAccessor, true);
        }

        private void TraceInner<T>(Func<T> paramsAccessor, bool deepClone) where T : class
        {
            if (IsCompleted)
            {
                return;
            }

            var clone = entry.Clone(deepClone);
            clone.AnonymousMethodInfo = paramsAccessor.GetAnonymousMethodInfo();
            clone.EventType = TraceEventType.Verbose;
            var extension = Log.WithParams(paramsAccessor);
            extension.ApplyTo(clone);
            Write(clone);
        }

        /// <summary>
        /// Starts the stopwatch if one is enabled.
        /// </summary>
        private void StartTiming()
        {
            if (Extension<Stopwatch>.IsEnabledFor(entry.CallingType))
            {
                stopwatch = entry.AddExtension<Stopwatch>();
                stopwatch.Start();
            }
        }

        private void Write(LogEntry entry)
        {
            if (requireConfirm)
            {
                buffer.Enqueue(entry);
            }
            else
            {
                Log.Write(entry);
            }
        }

        private class Confirmation
        {
            private object value;
            private readonly long? elapsedMilliseconds;

            public Confirmation(Func<object> accessor, long? elapsedMilliseconds)
            {
                Accessor = accessor;
                this.elapsedMilliseconds = elapsedMilliseconds;
            }

            private Func<object> Accessor { get; }

            internal object ResolvedValue => value ?? (value = Accessor.InvokeSafely());

            public string Value => ToString();

            public override string ToString() =>
                elapsedMilliseconds == null
                    ? ResolvedValue.ToLogString()
                    : $"{ResolvedValue.ToLogString()} (@{elapsedMilliseconds}ms)";

            public override int GetHashCode() => Accessor.Method.GetHashCode();
        }

        private class ConfirmationList : IEnumerable<Confirmation>
        {
            private readonly Dictionary<int, Confirmation> confirmations = new Dictionary<int, Confirmation>();

            static ConfirmationList()
            {
                Formatter<ConfirmationList>.ListExpansionLimit = 100;
            }

            public void Add(Confirmation confirmation)
            {
                lock (confirmations)
                {
                    confirmations[confirmation.GetHashCode()] = confirmation;
                }
            }

            public IEnumerator<Confirmation> GetEnumerator()
            {
                List<Confirmation> values;
                lock (confirmations)
                {
                    values = confirmations.Values.ToList();
                }
                return values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}