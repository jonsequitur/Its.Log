using System;
using System.Diagnostics;

namespace Its.Log.Instrumentation
{
    internal static class ObservableExtensions
    {
        public static IDisposable WriteToTrace(this IObservable<LogEntry> source)
        {
            return source.Subscribe(new TraceObserver());
        }
        
        public static IDisposable WriteToConsole(this IObservable<LogEntry> source)
        {
            return source.Subscribe(new ConsoleObserver());
        }

        public class TraceObserver : IObserver<LogEntry>
        {
            public void OnNext(LogEntry value)
            {
                Trace.WriteLine(value.ToLogString(), value.Category);
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }
        
        public class ConsoleObserver : IObserver<LogEntry>
        {
            public void OnNext(LogEntry value)
            {
                Console.WriteLine(value.ToLogString());
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }
    }
}