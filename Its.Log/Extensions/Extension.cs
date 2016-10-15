// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Its.Log.Instrumentation.Extensions
{
    /// <summary>
    /// Provides control over which extensions are enabled and for which classes.
    /// </summary>
    public class Extension
    {
        private readonly List<Func<LogEntry, object>> extendOnEnter = new List<Func<LogEntry, object>>();
        private readonly List<Func<LogEntry, object>> extendOnExit = new List<Func<LogEntry, object>>();

        /// <summary>
        /// Initializes the <see cref="Extension"/> class.
        /// </summary>
        static Extension()
        {
            Extension<Counter>.Disable();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Extension"/> class.
        /// </summary>
        protected internal Extension()
        {
        }

        protected internal static event EventHandler<EventArgs> EnableAllSignaled;

        /// <summary>
        /// Enables all known extension types.
        /// </summary>
        public static void EnableAll() => EnableAllSignaled?.Invoke(null, EventArgs.Empty);

        /// <summary>
        ///   Indicates to the log that execution is entering a region.
        /// </summary>
        /// <typeparam name = "T">The <see cref = "Type" /> of the anonymous type of <paramref name = "paramsAccessor" /> used to enclose parameters to be logged at this boundary.</typeparam>
        /// <param name = "paramsAccessor">An anonymous type enclosing parameters to be logged.</param>
        /// <returns>An <see cref = "ILogActivity" />.</returns>
        ///  /// <param name="requireConfirm">if set to <c>true</c>, then no log entries will be written if and until <see cref="ILogActivity.Confirm" /> is called on the returned log activity.</param>
        public ILogActivity Enter<T>(Func<T> paramsAccessor, bool requireConfirm = false) where T : class
        {
            Func<Func<T>, ILogActivity> enter = action =>
            {
                if (!Extension<Boundaries>.IsEnabledFor(
                    action.GetAnonymousMethodInfo().EnclosingType))
                {
                    return NullLogActivity.Instance;
                }
                var entry = new LogEntry<T>(action);

                ExtendLogEntry(entry, extendOnEnter);

                return new LogActivity(entry,
                                       requireConfirm,
                                       onComplete: e => ExtendLogEntry(e, extendOnExit));
            };

            return enter.InvokeSafely(paramsAccessor);
        }

        /// <summary>
        /// Indicates to the log that execution is entering a region.
        /// </summary>
        /// <param name="magicBarbell">An empty anonymous method which will not be called, i.e.: ( ) => { }</param>
        /// <returns>An <see cref="ILogActivity"/>.</returns>
        /// <remarks>The anonymous method is used to resolve the enclosing method and type without the use of a more expensive <see cref="StackFrame" />.</remarks>
        public ILogActivity Enter(Action magicBarbell)
        {
            Func<Action, ILogActivity> enter = action =>
            {
                var anonymousMethodInfo = action.GetAnonymousMethodInfo();
                if (!Extension<Boundaries>.IsEnabledFor(
                    anonymousMethodInfo.EnclosingType))
                {
                    return NullLogActivity.Instance;
                }

                var entry = new LogEntry(anonymousMethodInfo);

                ExtendLogEntry(entry, extendOnEnter);

                return new LogActivity(entry,
                                       onComplete: e => ExtendLogEntry(e, extendOnExit));
            };

            return enter.InvokeSafely(magicBarbell);
        }

        /// <summary>
        /// Adds an extension of type <typeparamref name="TExtension" /> to the <see cref="LogEntry"/> if extension type <typeparamref name="TExtension" /> is enabled.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension.</typeparam>
        /// <param name="extend">An action to set properties of the extension instance.</param>
        /// <returns></returns>
        public Extension With<TExtension>(Action<TExtension> extend)
            where TExtension : new()
        {
            if (Extension<TExtension>.IsEnabled)
            {
                if (Extension<TExtension>.ActivateOnEnter)
                {
                    extendOnEnter.Add(entry =>
                    {
                        var extension = new TExtension();

                        var applyOnEnter = extension as IApplyOnEnter;
                        applyOnEnter?.OnEnter(entry);

                        extend(extension);
                        
                        return extension;
                    });
                }

                if (Extension<TExtension>.ActivateOnExit)
                {
                    extendOnExit.Add(entry =>
                    {
                        var extension = new TExtension();

                        var applyOnExit = extension as IApplyOnExit;
                        applyOnExit?.OnExit(entry);

                        extend(extension);

                        return extension;
                    });
                }
            }

            return this;
        }

        /// <summary>
        /// Adds an extension of type <typeparamref name="TExtension" /> to the <see cref="LogEntry"/> if extension type <typeparamref name="TExtension" /> is enabled.
        /// </summary>
        public Extension With<TExtension>()
            where TExtension : new() =>
                With<TExtension>(delegate { });

        /// <summary>
        /// Writes the specified entry to the log.
        /// </summary>
        /// <param name="entry">The entry to be written.</param>
        public void Write(LogEntry entry)
        {
            ApplyTo(entry);
            Log.Write(entry);
        }

        internal void ApplyTo(LogEntry entry)
        {
            Action<LogEntry> action = logEntry => ExtendLogEntry(logEntry, extendOnEnter);
            action.InvokeSafely(entry);
        }

        /// <summary>
        /// Writes the specified object to the log.
        /// </summary>
        /// <param name="obj">The object to be written.</param>
        public void Write(object obj) => Write(new LogEntry(obj));

        /// <summary>
        /// Writes the specified object to the log.
        /// </summary>
        /// <param name="subject">The object to be written.</param>
        /// <param name="comment">A comment to provide context to the log entry.</param>
        public void Write(object subject, string comment) => Write(new LogEntry(subject) { Message = comment });

        private static void ExtendLogEntry(
            LogEntry logEntry,
            IEnumerable<Func<LogEntry, object>> extenders)
        {
            foreach (var extend in extenders)
            {
                var ext = extend(logEntry);

                var paramsExt = ext as Params;
                if (paramsExt != null)
                {
                    if (logEntry.AnonymousMethodInfo == null)
                    {
                        logEntry.AnonymousMethodInfo = paramsExt.ParamsAccessor.GetAnonymousMethodInfo();
                    }

                    if (Extension<Params>.IsEnabledFor(logEntry.CallingType))
                    {
                        logEntry.SetExtension(ext);
                    }
                }
                else
                {
                    logEntry.SetExtension(ext);
                }
            }
        }
    }
}