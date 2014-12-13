// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Provides methods for evaluating and describing exceptions.
    /// </summary>
    public static class ExceptionExtensions
    {
        private const string ExceptionDataKey = "ItsLog_ExceptionData_";
        internal const string ExceptionDataPrefix = "__Its_Log_";
        internal const string FullStackTraceKey = ExceptionDataPrefix + "StackTrace__";

        /// <summary>
        /// Returns all of the inner exceptions of an exception in a single sequence.
        /// </summary>
        /// <param name="exception">The exception.</param>
        internal static IEnumerable<Exception> InnerExceptions(this Exception exception)
        {
            if (exception == null)
            {
                yield break;
            }

            // aggregate exceptions require special treatment
            var aggregate = exception as AggregateException;
            if (aggregate != null)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    yield return inner;

                    foreach (var innerInner in inner.InnerExceptions())
                    {
                        yield return innerInner;
                    }
                }

                yield break;
            }

            // other exceptions are more straightforward
            var next = exception.InnerException;

            while (next != null)
            {
                yield return next;

                next = next.InnerException;
            }
        }

        /// <summary>
        /// Checks if an exception is considered fatal, i.e. cannot/should not be handled by an application.
        /// </summary>
        /// <param name="exception">Exception instance</param>
        /// <returns>True if exception is considered fatal, or false otherwise</returns>
        internal static bool IsFatal(this Exception exception)
        {
            return exception.IsItselfFatal() || exception.InnerExceptions().Any(IsItselfFatal);
        }

        private static bool IsItselfFatal(this Exception exception)
        {
            return (exception is ThreadAbortException ||
                    exception is AccessViolationException ||
                    (exception is OutOfMemoryException) && !(exception is InsufficientMemoryException));
        }

        /// <summary>
        /// Marks the exception as having been handled.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static TException MarkAsHandled<TException>(this TException exception)
            where TException : Exception
        {
            exception.Data["Handled"] = true;
            return exception;
        }

        /// <summary>
        /// Determines whether the exception has been handled.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>
        ///   <c>true</c> if the exception has been handled; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasBeenHandled(this Exception exception)
        {
            return exception.Data.Contains("Handled") &&
                   Equals(exception.Data["Handled"], true);
        }

        /// <summary>
        /// Add additional data to the exception as string for logging purpose. Anonymous type works the best.
        /// </summary>
        /// <typeparam name="TException">The type of the exception</typeparam>
        /// <param name="exception">The exception</param>
        /// <param name="obj">The object to be logged</param>
        /// <returns>The same exception</returns>
        public static TException WithData<TException>(this TException exception, object obj)
            where TException : Exception
        {
            // if the same key is already in the Data dictionary, append a number starting with 2
            // to the end of the key to create a new key/value pair
            int index = 2;
            string key = ExceptionDataKey;
            while (exception.Data.Contains(key))
            {
                key = ExceptionDataKey + index++;
            }

            exception.Data[key] = Formatter.Format(obj);
            return exception;
        }

        /// <summary>
        /// Annotates an exception with a full stack trace.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="exception">The exception.</param>
        /// <returns>The same exception instance.</returns>
        /// <remarks>This will add a full stack trace, including the incoming stack trace, to the exception's Data dictionary. When written out using ToLogString, the full stack trace will replace the exception's StackTrace property in the output. This is primarily useful when an exception is caught and swallowed, but it is still desirable to log the incoming stack trace.</remarks>
        public static TException WithFullStackTrace<TException>(this TException exception) where TException : Exception
        {
            exception.Data[FullStackTraceKey] = exception.StackTrace + new StackTrace();
            return exception;
        }
    }
}