// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Represents a series of associated events.
    /// </summary>
    public interface ILogActivity : IDisposable
    {
        /// <summary>
        /// Writes a log message associated with the current activity.
        /// </summary>
        void Trace(string comment);
        
        /// <summary>
        /// Logs variables associated with the current activity.
        /// </summary>
        void Trace<T>(Func<T> paramsAccessor) where T : class;
        
        /// <summary>
        /// Logs variables associated with the current activity and registers them to be logged again each time additional events are logged in the activity, so that their updated values will be recorded.
        /// </summary>
        void TraceAndWatch<T>(Func<T> paramsAccessor) where T : class;

        /// <summary>
        /// Completes the current activity.
        /// </summary>
        void Complete(Action magicBarbell);

        /// <summary>
        /// Writes out buffered entries in a log activity that was entered with requireConfirm set to true.
        /// </summary>
        void Confirm(Func<object> value = null);
    }

    public interface IApplyOnEnter : ILogExtension
    {
        void OnEnter(LogEntry logEntry);
    }

    public interface IApplyOnExit : ILogExtension
    {
        void OnExit(LogEntry logEntry);
    }

    public interface ILogExtension
    {
    }
}