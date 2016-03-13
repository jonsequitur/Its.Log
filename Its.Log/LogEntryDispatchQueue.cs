// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Its.Log.Instrumentation
{
    public class LogEntryDispatchQueue : AsyncDispatchQueue<LogEntry>
    {
        public LogEntryDispatchQueue(
            Func<LogEntry, Task> send,
            IObservable<LogEntry> logEvents = null) :
                base(send, logEvents ?? Log.Events())
        {
        }
    }
}