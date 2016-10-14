// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;

namespace Its.Log.Instrumentation.UnitTests
{
    public static class TestHelper
    {
        public static IDisposable LogToConsole()
        {
            return Log.Events().Subscribe(e => Console.WriteLine(e.ToLogString()));
        }

        public static IObservable<LogEntry> InternalErrors()
        {
            return Observable
                .FromEventPattern<InstrumentationEventArgs>(
                h => Log.InternalErrors += h, h => Log.InternalErrors -= h)
                .Select(e => e.EventArgs.LogEntry);
        }

        public static IDisposable SubscribeToLogEvents(this IObserver<LogEntry> observer)
        {
            var subscription = Log.Events().Subscribe(observer);
            return subscription;
        }

        public static IDisposable SubscribeToLogInternalErrors(this IObserver<LogEntry> observer)
        {
            var subscription = InternalErrors().Subscribe(observer);
            return subscription;
        }

        public static IDisposable OnEntryPosted(Action<LogEntry> doSomething)
        {
            return Observable
                .FromEventPattern<InstrumentationEventArgs>(
                h => Log.EntryPosted += h, h => Log.EntryPosted -= h)
                .Select(e => e.EventArgs.LogEntry)
                .Subscribe(doSomething);
        }
         
        public static IDisposable LogToConsole<T>(this IObservable<T> source)
        {
            return source.Subscribe(e => Console.WriteLine(e.ToLogString()));
        }

        public static T Trace<T>(this T value)
        {
            Console.WriteLine(value.ToLogString());
            return value;
        }
    }
}