// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Its.Log.Instrumentation.Extensions;
using Its.Recipes;

namespace Its.Log.Instrumentation
{
    /// <summary>
    ///     Provides access to rules configuration and trace/log write methods.
    /// </summary>
    public static class Log
    {
        private static readonly FormatterSet formatters = new FormatterSet();

        /// <summary>
        ///     Occurs when a log entry is posted.
        /// </summary>
        public static event EventHandler<InstrumentationEventArgs> EntryPosted;

        /// <summary>
        ///     Occurs when any exception is thrown in a delegate passed into Its.Log.
        /// </summary>
        public static event EventHandler<InstrumentationEventArgs> InternalErrors;

        /// <summary>
        ///     Unsubscribes all subscribers to the <see cref="Log.EntryPosted" /> event.
        /// </summary>
        internal static void UnsubscribeAllFromEntryPosted()
        {
            var handler = EntryPosted;
            if (handler == null)
            {
                return;
            }

            var subscribers = EntryPosted.GetInvocationList();

            foreach (var d in subscribers)
            {
                EntryPosted -= (d as EventHandler<InstrumentationEventArgs>);
            }
        }

        /// <summary>
        ///     Gets the global formatter set, which is used by <see cref="ToLogString{T}" />.
        /// </summary>
        [Obsolete("Please use the Formatter class instead.")]
        public static FormatterSet Formatters
        {
            get { return formatters; }
        }

        private static Func<Exception, bool> canThrowWhen = ex => ex.IsFatal();

        /// <summary>
        ///     Gets or sets a function indicating when instrumentation code is permitted to throw an exception to instrumented application code.
        /// </summary>
        public static Func<Exception, bool> CanThrowWhen
        {
            get { return canThrowWhen; }
            set { canThrowWhen = value ?? (ex => ex.IsFatal()); }
        }

        internal static bool ShouldThrow(this Exception exception)
        {
            return canThrowWhen(exception);
        }

        /// <summary>
        ///     Indicates to the log that execution is entering a region.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type" /> of the anonymous type of <paramref name="paramsAccessor" /> used to enclose parameters to be logged at this boundary.
        /// </typeparam>
        /// <param name="paramsAccessor">An anonymous type enclosing parameters to be logged.</param>
         /// <returns>
        ///     An <see cref="ILogActivity" /> that, when disposed, writes out the closing log entry, including the updated state of the return value of <paramref name="paramsAccessor" />.
        /// </returns>
        public static ILogActivity Enter<T>(Func<T> paramsAccessor, bool requireConfirm = false) where T : class
        {
            Func<Func<T>, ILogActivity> enter = action =>
                {
                    if (
                        !Extension<Boundaries>.IsEnabledFor(
                            action.GetAnonymousMethodInfo().EnclosingType))
                    {
                        return NullLogActivity.Instance;
                    }
                    var entry = new LogEntry<T>(action);
                    return new LogActivity(entry, requireConfirm);
                };
            return enter.InvokeSafely(paramsAccessor);
        }

        /// <summary>
        ///     Indicates that execution is entering a region.
        /// </summary>
        /// <param name="magicBarbell">An empty anonymous method which will not be called, i.e.: ( ) => { }</param>
        /// <returns>
        ///     An <see cref="ILogActivity" />.
        /// </returns>
        /// <remarks>
        ///     The anonymous method is used to resolve the enclosing method and type without the use of a more expensive
        ///     <see
        ///         cref="StackFrame" />
        ///     .
        /// </remarks>
        public static ILogActivity Enter(Action magicBarbell, bool requireConfirm = false)
        {
            Func<Action, ILogActivity> enter = action =>
                {
                    var anonymousMethodInfo = action.GetAnonymousMethodInfo();
                    if (!Extension<Boundaries>.IsEnabledFor(anonymousMethodInfo.EnclosingType))
                    {
                        return NullLogActivity.Instance;
                    }
                    var entry = new LogEntry(anonymousMethodInfo);
                    return new LogActivity(entry, requireConfirm);
                };
            return enter.InvokeSafely(magicBarbell);
        }

        /// <summary>
        ///     Indicates that execution is exiting a region.
        /// </summary>
        /// <param name="activity">The log activity to be ended.</param>
        public static void Exit(ILogActivity activity)
        {
            if (activity != null)
            {
                activity.Dispose();
            }
        }

        /// <summary>
        /// Returns an observable sequence of all log entries posted to Its.Log.
        /// </summary>
        public static IObservable<LogEntry> Events()
        {
            return new LogEventsObservable();
        }
        
        /// <summary>
        /// Returns an observable sequence of all log entries posted to Its.Log whose handlers throw an exception.
        /// </summary>
        public static IObservable<LogEntry> InternalErrorEvents()
        {
            return new LogInternalErrorsObservable();
        }

        private class LogEventsObservable : IObservable<LogEntry>
        {
            public IDisposable Subscribe(IObserver<LogEntry> observer)
            {
                EventHandler<InstrumentationEventArgs> handler = (sender, args) => observer.OnNext(args.LogEntry);
                EntryPosted += handler;
                return new AnonymousDisposable(() => { EntryPosted -= handler; });
            }
        }

        private class LogInternalErrorsObservable : IObservable<LogEntry>
        {
            public IDisposable Subscribe(IObserver<LogEntry> observer)
            {
                EventHandler<InstrumentationEventArgs> handler = (sender, args) => observer.OnNext(args.LogEntry);
                InternalErrors += handler;
                return new AnonymousDisposable(() => { InternalErrors -= handler; });
            }
        }

        /// <summary>
        ///     Formats an object to a string based on the framework configuration for its <see cref="Type" />.
        /// </summary>
        /// <param name="obj">The object to be formatted.</param>
        public static string ToLogString<T>(this T obj)
        {
            return Formatter.Format(obj);
        }

        /// <summary>
        ///     Adds an extension of the specified type to the log entry.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension.</typeparam>
        public static Extension With<TExtension>()
            where TExtension : new()
        {
            var chain = new Extension();
            chain.With<TExtension>();
            return chain;
        }

        /// <summary>
        ///     Adds an extension of the specified type to the log entry.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension.</typeparam>
        public static Extension With<TExtension>(Action<TExtension> extend)
            where TExtension : new()
        {
            return new Extension().With(extend);
        }

        /// <summary>
        ///     Adds parameters to the log entry.
        /// </summary>
        /// <param name="paramsAccessor">The parameters accessor, generally a lambda returning an anonymous method specifying the parameters to be logged.</param>
        /// <returns></returns>
        public static Extension WithParams<T>(Func<T> paramsAccessor) where T : class
        {
            return With<Params<T>>(ext => ext.SetAccessor(paramsAccessor));
        }

        /// <summary>
        ///     Writes the specified message to the log.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        public static void Write(string message)
        {
            Write(new LogEntry(message));
        }

        /// <summary>
        ///     Writes the specified object to the log.
        /// </summary>
        /// <param name="subject">The object to be written.</param>
        public static void Write<T>(T subject)
        {
            Write(new LogEntry<T>(subject) as LogEntry);
        }

        /// <summary>
        ///     Writes the specified object to the log.
        /// </summary>
        /// <param name="getSubject">A function that returns the object to be written.</param>
        public static void Write<T>(Func<T> getSubject)
        {
            var entry = new LogEntry<T>(getSubject);
            Write((LogEntry) entry);
        }

        /// <summary>
        ///     Writes the specified object to the log.
        /// </summary>
        /// <param name="getSubject">A function that returns the object to be written.</param>      
        /// /// <param name="comment">A comment to provide context to the log entry.</param>
        public static void Write<T>(Func<T> getSubject, string comment)
        {
            var entry = new LogEntry<T>(getSubject)
                {
                    Message = comment
                };
            Write((LogEntry) entry);
        }

        /// <summary>
        ///     Writes the specified object to the log.
        /// </summary>
        /// <param name="subject">The object to be written.</param>
        /// <param name="comment">A comment to provide context to the log entry.</param>
        public static void Write<T>(T subject, string comment)
        {
            var entry = new LogEntry<T>(subject) {Message = comment};
            Write((LogEntry) entry);
        }

        /// <summary>
        ///     Writes the specified entry to the log.
        /// </summary>
        /// <param name="entry">The entry to be written.</param>
        /// <remarks>
        ///     All overloads of Write ultimately call this method.
        /// </remarks>
        public static void Write(LogEntry entry)
        {
            var handler = EntryPosted;
            if (handler == null)
            {
                return;
            }

            // prevent re-entrancy
            if (entry.HasBeenPosted)
            {
                return;
            }
            entry.HasBeenPosted = true;

            foreach (EventHandler<InstrumentationEventArgs> subscriber in EntryPosted.GetInvocationList())
            {
                try
                {
                    subscriber(typeof (Log), new InstrumentationEventArgs(entry));
                }
                catch (Exception exceptionFromHandler)
                {
                    exceptionFromHandler.RaiseErrorEvent();
                    if (exceptionFromHandler.ShouldThrow())
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        ///     Writes the specified entry to the log.
        /// </summary>
        /// <param name="entry">The entry to be written.</param>
        /// <remarks>
        ///     All overloads of Write ultimately call this method.
        /// </remarks>
        public static void Write<T>(LogEntry<T> entry)
        {
            Write((LogEntry) entry);
        }

        internal static void RaiseErrorEvent(this Exception ex)
        {
            var handler = InternalErrors;
            if (handler != null)
            {
                try
                {
                    handler(typeof (Log), new InstrumentationEventArgs(new LogEntry(ex)));
                }
                catch (Exception exceptionFromHandler)
                {
                    // don't let any exception propagate from the subscriber back to the caller.
                    // as a last resort, write to the console
                    Console.WriteLine(exceptionFromHandler);
                    if (exceptionFromHandler.ShouldThrow())
                    {
                        throw;
                    }
                }
            }
        }
    }
}