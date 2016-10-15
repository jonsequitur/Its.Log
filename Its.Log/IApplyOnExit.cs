// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// A log extension that applies when a log activity is exited.
    /// </summary>
    public interface IApplyOnExit : ILogExtension
    {
        /// <summary>
        /// Called when <see cref="Log.Enter" /> generates its final <see cref="LogEntry" />.
        /// </summary>
        /// <param name="logEntry">The log entry.</param>
        void OnExit(LogEntry logEntry);
    }
}