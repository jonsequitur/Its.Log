// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Provides extensions to <see cref="LogEntry" />.
    /// </summary>
    public static class LogEntryExtensions
    {
        /// <summary>
        /// Determines whether the subject is an instance of T or a type that is assignable to T. 
        /// </summary>
        /// 
        public static bool SubjectIs<T>(this LogEntry entry) =>
            entry?.Subject is T;

        /// <summary>
        /// Determines whether the subject is an instance of T or a type that is assignable to T, and also matches the specified predicate. 
        /// </summary>
        public static bool SubjectIs<T>(this LogEntry entry, Predicate<T> @if) =>
            entry.SubjectIs<T>() && @if((T) entry.Subject);
    }
}