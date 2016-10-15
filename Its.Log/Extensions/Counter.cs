// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Its.Log.Instrumentation.Extensions
{
    /// <summary>
    /// Counts the number of calls made on a method.
    /// </summary>
    public class Counter
    {
        private static readonly ConcurrentDictionary<string, Counter> counters = new ConcurrentDictionary<string, Counter>();

        private long count;

        /// <summary>
        /// Gets the call count for the current <see cref="Counter" />.
        /// </summary>
        public long Count => count;

        /// <summary>
        ///   Increments the counter by one.
        /// </summary>
        public void Increment() => Interlocked.Increment(ref count);

        /// <summary>
        /// Clears all.
        /// </summary>
        public static void ResetAll() => counters.Clear();

        /// <summary>
        /// Gets the <see cref="Counter" /> for the specified method.
        /// </summary>
        /// <typeparam name="T">The type on which the method is found</typeparam>
        /// <param name="methodName">The name of the method.</param>
        /// <returns></returns>
        public static Counter For<T>(string methodName)
        {
            return For(typeof (T), methodName);
        }

        /// <summary>
        /// Gets the <see cref="Counter" /> for the specified method.
        /// </summary>
        public static Counter For<T>(Expression<Action<T>> method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            var action = method.Body as MethodCallExpression;
            var actionName = action.Method.Name;
            return For<T>(actionName);
        }

        /// <summary>
        ///   Gets the <see cref="Counter" /> for the specified method.
        /// </summary>
        public static Counter For(Type type, string methodName)
        {
            var key = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", type, methodName);
            return counters.GetOrAdd(key, _ => new Counter());
        }
    }
}