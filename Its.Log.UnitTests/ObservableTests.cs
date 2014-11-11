using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using FluentAssertions;
using Its.Recipes;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class ObservableTests
    {
        [Test]
        public void Events_are_sent_to_multiple_subscribed_observers()
        {
            var callCount1 = 0;
            var callCount2 = 0;

            using (Log.Events().Subscribe(_ => callCount1++))
            using (Log.Events().Subscribe(_ => callCount2++))
            using (var activity = Log.Enter(() => { }))
            {
                activity.Trace(() => "");
                Log.Write(() => "");
            }

            callCount1.Should().Be(4);
            callCount2.Should().Be(4);
        }

        [Test]
        public void Events_are_no_longer_sent_once_the_subscription_is_disposed()
        {
            int callCount = 0;
            var numberOfEntries = Any.PositiveInt(100);

            using (Log.Events().Subscribe(onNext: _ => callCount++))
            {
                Enumerable.Range(1, numberOfEntries).ForEach(Log.Write);
            }

            Enumerable.Range(1, Any.PositiveInt(50)).ForEach(Log.Write);

            callCount.Should().Be(numberOfEntries);
        }

        [Test]
        public void The_LogEntry_that_is_written_is_sent_to_the_observer()
        {
            var sent = new LogEntry("hello!");
            LogEntry received = null;

            using (Log.Events().Subscribe(onNext: e => received = e))
            {
                Log.Write(sent);
            }

            received.Should().BeSameAs(sent);
        }

        [Test]
        public void When_an_observer_throws_then_the_subscription_is_not_broken_and_no_exception_is_passed_to_onError()
        {
            // this is a bit of a departure from Rx norms but it's in keeping with the spirit of Its.Log never to propagate diagnostics-related exceptions into application code
            var callCount = 0;
            var errorCount = 0;

            using (Log.Events().Subscribe(onNext: e =>
            {
                callCount++;
                if (e.GetSubject<int>() > 5)
                {
                    throw new Exception("oops!");
                }
            }, onError: e => { errorCount++; }))
            {
                Enumerable.Range(1, 10).ForEach(Log.Write);
            }

            callCount.Should().Be(10);
            errorCount.Should().Be(0);
        }

        [Test]
        public void When_an_observer_throws_then_the_exception_is_published_via_InternalErrors()
        {
            var errorCount = 0;
            var numberOfEvents = Any.PositiveInt(100);

            using (Log.Events().Subscribe(onNext: e => { throw new Exception("oops!"); }))
            using (Log.InternalErrorEvents().Subscribe(onNext: e => { errorCount++; }))
            {
                Enumerable.Range(1, numberOfEvents).ForEach(Log.Write);
            }

            errorCount.Should().Be(numberOfEvents);
        }

        [NUnit.Framework.Ignore("Scenario under development")]
        [Test]
        public void WriteToConsole_can_be_used_to_send_log_entries_to_the_console()
        {
            // FIX (WriteToConsole_can_be_used_to_send_log_entries_to_the_console) write test
            Assert.Fail("Test not written yet.");
        }

        [NUnit.Framework.Ignore("Scenario under development")]
        [Test]
        public void WriteToTrace_can_be_used_to_send_log_entries_to_trace()
        {
            var traces = new List<string>();
            var name = Any.FullName();

            using (var listener = TraceListener.Create(traces.Add))
            using (Log.Events().WriteToTrace())
            {
                Log.Write(name);
            }

            traces.Should().Contain(name);
        }

        [NUnit.Framework.Ignore("Scenario under development")]
        [Test]
        public void When_WriteToTrace_is_called_multiple_times_then_duplicate_log_entries_are_not_written()
        {
            

            // FIX (When_WriteToTrace_is_called_multiple_times_then_) write test
            Assert.Fail("Test not written yet.");
        }

        public static class TraceListener
        {
            public static IDisposable Create(Action<string> write)
            {
                var listener = new AnonymousTraceListener(write);
                Trace.Listeners.Add(listener);
                return Disposable.Create(() => Trace.Listeners.Remove(listener));
            }

            private class AnonymousTraceListener : System.Diagnostics.TraceListener
            {
                private readonly Action<string> write;

                public AnonymousTraceListener(Action<string> write)
                {
                    this.write = write;
                }

                public override void Write(string message)
                {
                    WriteLine(message);
                }

                public override void WriteLine(string message)
                {
                    write(message);
                }
            }
        }
    }
}