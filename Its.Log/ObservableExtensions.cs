// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Its.Log.Instrumentation
{
    public static class ObservableExtensions
    {
        public static IDisposable WriteToTrace(this IObservable<LogEntry> source) =>
            source.Subscribe(new TraceObserver());

        public static IDisposable WriteToConsole(this IObservable<LogEntry> source) =>
            source.Subscribe(new ConsoleObserver());

        public class TraceObserver : IObserver<LogEntry>
        {
            public void OnNext(LogEntry value) =>
                Trace.WriteLine(value.ToLogString(), value.Category);

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }

        public class ConsoleObserver : IObserver<LogEntry>
        {
            public void OnNext(LogEntry value) =>
                Console.WriteLine(value.ToLogString());

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }
    }
}