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
        private readonly List<KeyValuePair<Type, Func<LogEntry, object>>> extensionGetters =
            new List<KeyValuePair<Type, Func<LogEntry, object>>>();

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
        public static void EnableAll()
        {
            var handler = EnableAllSignaled;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        /// <summary>
        ///   Indicates to the log that execution is entering a region.
        /// </summary>
        /// <typeparam name = "T">The <see cref = "Type" /> of the anonymous type of <paramref name = "paramsAccessor" /> used to enclose parameters to be logged at this boundary.</typeparam>
        /// <param name = "paramsAccessor">An anonymous type enclosing parameters to be logged.</param>
        /// <returns>An <see cref = "ILogActivity" />.</returns>
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
                                                        ExtendLogEntry(entry);
                                                        return new LogActivity(entry, requireConfirm);
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
                                                       AnonymousMethodInfo anonymousMethodInfo = action.GetAnonymousMethodInfo();
                                                       if (!Extension<Boundaries>.IsEnabledFor(
                                                           anonymousMethodInfo.EnclosingType))
                                                       {
                                                           return NullLogActivity.Instance;
                                                       }
                                                       var entry = new LogEntry(anonymousMethodInfo);
                                                       ExtendLogEntry(entry);
                                                       return new LogActivity(entry);
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
            where TExtension : class, new()
        {
            if (Extension<TExtension>.IsEnabled)
            {
                extensionGetters.Add(
                    new KeyValuePair<Type, Func<LogEntry, object>>(
                        typeof (TExtension),
                        entry =>
                        {
                            var extension = new TExtension();
                            extend(extension);
                            return extension;
                        }));
            }
            return this;
        }

        /// <summary>
        /// Adds an extension of type <typeparamref name="TExtension" /> to the <see cref="LogEntry"/> if extension type <typeparamref name="TExtension" /> is enabled.
        /// </summary>
        public Extension With<TExtension>()
            where TExtension : class, new()
        {
            if (Extension<TExtension>.IsEnabled)
            {
                extensionGetters.Add(
                    new KeyValuePair<Type, Func<LogEntry, object>>(
                        typeof (TExtension),
                        entry =>
                            {
                                var extension = new TExtension();
                                return extension;
                            }));
            }
            return this;
        }

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
            Action<LogEntry> action = ExtendLogEntry;
            action.InvokeSafely(entry);
        }

        /// <summary>
        /// Writes the specified object to the log.
        /// </summary>
        /// <param name="obj">The object to be written.</param>
        public void Write(object obj)
        {
            Write(new LogEntry(obj));
        }

        /// <summary>
        /// Writes the specified object to the log.
        /// </summary>
        /// <param name="subject">The object to be written.</param>
        /// <param name="comment">A comment to provide context to the log entry.</param>
        public void Write(object subject, string comment)
        {
            var entry = new LogEntry(subject) { Message = comment };
            Write(entry);
        }

        private void ExtendLogEntry(LogEntry e)
        {
            foreach (var extensionGetter in extensionGetters)
            {
                var ext = extensionGetter.Value(e);

                var paramsExt = ext as Params;
                if (paramsExt != null)
                {
                    if (e.AnonymousMethodInfo == null)
                    {
                        e.AnonymousMethodInfo = paramsExt.ParamsAccessor.GetAnonymousMethodInfo();
                    }

                    if (Extension<Params>.IsEnabledFor(e.CallingType))
                    {
                        e.SetExtension(ext);
                    }
                }
                else
                {
                    e.SetExtension(ext);
                }
            }
        }
    }
}