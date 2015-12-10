// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Its.Log.Instrumentation.Extensions;
using Moq;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class LogActivityTests
    {
        [SetUp]
        public void SetUp()
        {
            Extension.EnableAll();
            Log.UnsubscribeAllFromEntryPosted();
        }

        [Test]
        public void Calling_Complete_then_Dispose_on_LogActivity_does_not_generate_second_log_event()
        {
            var observer = new Mock<IObserver<LogEntry>>();
            observer
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()));

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(observer.Object))
            {
                var activity = Log.Enter(() => { });
                activity.Complete(() => { });
                activity.Dispose();
            }

            observer.Verify(
                o => o.OnNext(It.IsAny<LogEntry>()),
                Times.Exactly(2));
        }

        [Test]
        public virtual void Calling_complete_twice_onLogActivity_does_not_generate_second_log_event()
        {
            var observer = new Mock<IObserver<LogEntry>>();
            observer
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()));

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(observer.Object))
            {
                var activity = Log.Enter(() => { });
                activity.Complete(() => { });
                activity.Complete(() => { });
            }

            observer.Verify(
                o => o.OnNext(It.IsAny<LogEntry>()),
                Times.Exactly(2));
        }

        [Test]
        public void Calling_dispose_twice_on_LogActivity_does_not_generate_second_log_event()
        {
            var observer = new Mock<IObserver<LogEntry>>();
            observer.Setup(o => o.OnNext(It.IsAny<LogEntry>()));

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(observer.Object))
            {
                var activity = Log.Enter(() => { });
                activity.Dispose();
                activity.Dispose();
            }

            var methodName = MethodBase.GetCurrentMethod().Name;
            observer.Verify(
                o => o.OnNext(It.Is<LogEntry>(e => e.EventType == TraceEventType.Stop &&
                                                   e.CallingMethod == methodName)),
                Times.Once());
        }

        [Test]
        public void Calling_trace_on_disposed_LogActivity_does_not_generate_log_event()
        {
            var observer = new Mock<IObserver<LogEntry>>();
            observer
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()));

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(observer.Object))
            {
                var activity = Log.Enter(() => { });
                activity.Dispose();
                activity.Trace(() => new { something = "something" });
            }

            observer.Verify(
                o => o.OnNext(It.IsAny<LogEntry>()),
                Times.Exactly(2));
        }

        [Test]
        public virtual void Calling_trace_on_disposed_LogActivity_does_not_throw()
        {
            Action action;

            using (var activity = Log.Enter(() => { }))
            {
                action = () => activity.Trace("here i am");
            }

            action();
        }

        [Test]
        public void Trace_will_not_add_params_to_activity()
        {
            LogEntry entry = null;
            using (Log.Events().Subscribe(e => entry = e))
            {
                using (var activity = Log.Enter(() => { }))
                {
                    entry.Params.Count().Should().Be(0);
                    activity.Trace(() => new { First = true, Value = "First" });
                    entry.Params.Count().Should().Be(1);
                    dynamic param = entry.Params.Single();
                    string value = param.Values.Value;
                    value.Should().Be("First");

                    activity.Trace(() => new { Second = true, Value = "Second" });
                    entry.Params.Count().Should().Be(1);
                    param = entry.Params.Single();
                    value = param.Values.Value;
                    value.Should().Be("Second");
                }
                entry.Params.Count().Should().Be(0);
            }
        }

        [Test]
        public void Can_add_params_to_LogActivity_after_instantiation_via_trace()
        {
            LogEntry lastEntry = null;

            var first = "one";
            var second = "two";

            using (Log.Events().Subscribe(e => lastEntry = e))
            using (var activity = Log.Enter(() => new { first, second }))
            {
                lastEntry.ToLogString().Should().Contain("one");
                lastEntry.ToLogString().Should().Contain("two");

                second += "2";
                var third = "three";
                var fourth = "FOUR";

                activity.TraceAndWatch(() => new { third });
                lastEntry.ToLogString().Should().Contain("two2");
                lastEntry.ToLogString().Should().Contain("three");

                activity.Trace(() => new { fourth });
                lastEntry.ToLogString().Should().Contain("FOUR");
                lastEntry.ToLogString().Should().Contain("two2");
                lastEntry.ToLogString().Should().Contain("three");

                activity.Trace(() => "five");
                lastEntry.ToLogString().Should().NotContain("FOUR");
            }
        }

        [Test]
        public virtual void LogActivity_created_with_LogEntry_having_null_calling_type_does_not_throw()
        {
            var entry = new LogEntry("a message");
            Assert.IsNull(entry.CallingType);
            var activity = new LogActivity(entry);
        }

        [Test]
        public virtual void LogActivity_trace_outputs_updated_parameter_values()
        {
            var log = "";

            var anInt = 1;
            var aString = "a";

            using (TestHelper.OnEntryPosted(e => log += e.ToLogString()))
            using (var activity = Log.Enter(() => new { anInt, aString }))
            {
                StringAssert.Contains("anInt = 1", log);
                StringAssert.Contains("aString = a", log);

                anInt = 2;
                aString = "b";

                activity.Trace("here i am");

                StringAssert.Contains("anInt = 2", log);
                StringAssert.Contains("aString = b", log);
            }
        }

        [Test]
        public virtual void Perf_timing_can_be_globally_disabled()
        {
            try
            {
                Extension<Stopwatch>.Disable();
                var observer = new Mock<IObserver<LogEntry>>();
                observer.Setup(
                    w => w.OnNext(
                        It.Is(
                            (LogEntry e) =>
                                e.EventType == TraceEventType.Stop && e.HasExtension<Stopwatch>() == false)));

                using (observer.Object.SubscribeToLogEvents())
                using (Log.Enter(() => { }))
                {
                }

                observer.VerifyAll();
            }
            finally
            {
                Extension<Stopwatch>.Enable();
            }
        }

        [Test]
        public virtual void StopwatchExtension_records_amount_of_time_from_activity_enter()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.Enter(() => { }))
            {
                Thread.Sleep(1000);
            }

            log.Should().Contain(e => e.EventType == TraceEventType.Stop &&
                                      e.GetExtension<Stopwatch>().ElapsedMilliseconds >= 900 &&
                                      e.GetExtension<Stopwatch>().IsRunning == false);
        }

        [Test]
        public virtual void Writes_on_enter_and_exit()
        {
            var observer = new Mock<IObserver<LogEntry>>();
            observer.Setup(w => w.OnNext(It.IsAny<LogEntry>()));

            using (observer.Object.SubscribeToLogEvents())
            using (Log.Enter(() => { }))
            {
            }

            observer.Verify(o => o.OnNext(It.Is<LogEntry>(e => e.EventType == TraceEventType.Start)));
            observer.Verify(o => o.OnNext(It.Is<LogEntry>(e => e.EventType == TraceEventType.Stop)));
        }

        [Test]
        public void When_confirmation_is_required_then_by_default_LogActivity_doesnt_write_out_logs()
        {
            var log = new List<LogEntry>();
            var step = 1;

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => new { step }, requireConfirm: true))
            {
                activity.Trace("a trace");
                activity.Trace(() => new { step });
            }

            log.Should().BeEmpty();
        }

        [Test]
        public void When_confirmation_is_required_then_by_calling_confirm_writes_out_buffered_logs()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }, requireConfirm: true))
            {
                activity.Trace("a trace");

                activity.Trace(() => "another");

                activity.Confirm();

                log.Count().Should().Be(3);
            }

            log.Count().Should().Be(4);
        }

        [Test]
        public void When_Confirm_is_called_then_entries_are_written_with_the_correctly_captured_values()
        {
            var log = new List<LogEntry>();
            var step = 1;

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => new { step }, requireConfirm: true))
            {
                step++;
                activity.Trace("a trace");

                step++;
                activity.Trace(() => new { step });

                Console.WriteLine("before confirm: " + log.ToLogString());

                activity.Confirm();

                step++;
                log.Count().Should().Be(3);
            }

            Console.WriteLine("after confirm: " + log.ToLogString());

            log.ElementAt(0).ToString().Should().Contain("step = 1");
            log.ElementAt(1).ToString().Should().Contain("step = 2");
            log.ElementAt(2).ToString().Should().Contain("step = 3");
            log.ElementAt(3).ToString().Should().Contain("step = 4");
        }

        [Test]
        public void When_Confirm_is_called_then_entries_are_written_with_the_correctly_captured_timestamps()
        {
            var log = new List<LogEntry>();
            DateTime confirmTime;

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }, requireConfirm: true))
            {
                activity.Trace("a trace");
                Thread.Sleep(2000);
                confirmTime = DateTime.UtcNow;
                Console.WriteLine(new { confirmTime });

                activity.Confirm();
            }

            Console.WriteLine("after confirm: " + log.ToLogString());

            log.ElementAt(0)
               .TimeStamp
               .AddSeconds(1)
               .Should()
               .BeBefore(confirmTime);
        }

        [Test]
        public void When_Confirm_called_more_than_once_then_it_has_no_effect()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }, requireConfirm: true))
            {
                activity.Trace("a trace");

                activity.Confirm();
                activity.Confirm();

                log.Count().Should().Be(2);
            }

            log.Count().Should().Be(3);
        }

        [Test]
        public void Confirm_can_be_used_to_verify_that_an_activity_completed_successfully_by_writing_a_value_to_be_included_on_exit()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }, requireConfirm: true))
            {
                activity.Confirm(() => new { a = "b" });
            }

            log.Count().Should().Be(2);
            log.Last().ToString().Should().Match(@"*Confirmed = { { a = b } (@*ms) }*");
        }

        [Test]
        public void Confirm_can_be_used_to_track_checkpoints_within_a_method()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                activity.Confirm(() => "getting started");
                activity.Confirm(() => "almost there...");
                activity.Confirm(() => "done!");
            }

            log.Last()
               .ToString()
               .Should()
               .Match(@"*Confirmed = { getting started (@*ms), almost there... (@*ms), done! (@*ms) }*");
        }

        [Test]
        public void Confirm_can_be_used_to_track_checkpoints_within_an_iteration_block_without_writing_additional_entries()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                for (var i = 0; i < 100; i++)
                {
                    activity.Confirm(() => i);
                }
            }

            log.Last().ToString().Should().NotMatch(@"*Confirmed = { 99 (@*ms) }*");
            log.Last().ToString().Should().Match(@"*Confirmed = { 100 (@*ms) }*");
        }

        [Test]
        public void Confirm_can_be_used_to_track_orthongonal_checkpoints()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }, requireConfirm: true))
            {
                for (var i = 0; i < 100; i++)
                {
                    activity.Confirm(() => new { processed = i });
                }
                activity.Confirm(() => new { completed = true });
            }

            log.Last().ToString().Should().Contain("processed = 100");
            log.Last().ToString().Should().Contain("completed = True");
        }

        [Test]
        public async Task Confirm_is_threadsafe()
        {
            var barrier = new Barrier(10);
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 1);
                    Console.WriteLine("   activity.Confirm(() => 1)");
                }).Start();
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 2);
                    Console.WriteLine("   activity.Confirm(() => 2)");
                }).Start();
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 3);
                    Console.WriteLine("   activity.Confirm(() => 3)");
                }).Start();
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 4);
                    Console.WriteLine("   activity.Confirm(() => 4)");
                }).Start();
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 5);
                    Console.WriteLine("   activity.Confirm(() => 5)");
                }).Start();
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 6);
                    Console.WriteLine("   activity.Confirm(() => 6)");
                }).Start();
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 7);
                    Console.WriteLine("   activity.Confirm(() => 7)");
                }).Start();
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 8);
                    Console.WriteLine("   activity.Confirm(() => 8)");
                }).Start();
                new Thread(() =>
                {
                    barrier.SignalAndWait(5000);
                    activity.Confirm(() => 9);
                    Console.WriteLine("   activity.Confirm(() => 9)");
                }).Start();

                barrier.SignalAndWait(5000);
                activity.Confirm(() => 10);
                Console.WriteLine("   activity.Confirm(() => 10)");
            }

            log.Last().Confirmations.Count().Should().Be(10);
        }

        [Test]
        public void When_Confirm_is_called_with_no_args_and_not_required_then_it_is_indicated_in_the_exit_log_entry()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }, requireConfirm: false))
            {
                activity.Confirm();
            }

            log.Last().ToString().Should().Match(@"*Confirmed = { True (@*ms) }*");
        }

        [Test]
        public void When_there_are_a_large_number_of_Confirm_calls_they_are_not_truncated_in_the_log_output()
        {
            var log = new List<LogEntry>();
            Formatter.ListExpansionLimit = 4;

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                activity.Confirm(() => "one");
                activity.Confirm(() => "two");
                activity.Confirm(() => "three");
                activity.Confirm(() => "four");
                activity.Confirm(() => "five");
            }

            log.Last().ToString().Should().Contain("five");
        }

        [Test]
        public async Task When_Confirm_delegate_throws_then_the_exception_is_handled()
        {
            FileInfo fileInfo = null;
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                activity.Confirm(() => fileInfo.Name);
            }

            log.Should().ContainSingle(e => e.EventType == TraceEventType.Stop);
            log.Single(e => e.EventType == TraceEventType.Stop)
               .Confirmations
               .Should()
               .ContainSingle(c => c is NullReferenceException);
        }

        [Test]
        public void Activity_Confirm_outputs_include_timings()
        {
            var log = new List<LogEntry>();
            Formatter.ListExpansionLimit = 4;

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                activity.Confirm(() => "one");
                Thread.Sleep(1000);
                activity.Confirm(() => "two");
            }

            log.Last().ToString().Should().Match(@"*one (@*ms), two (@1*ms)*");
        }

        [Test]
        public void Iterative_Activity_Confirm_captures_timing_of_final_confirmation()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                for (var i = 0; i < 100; i++)
                {
                    if (i == 99)
                    {
                        Thread.Sleep(1000);
                    }
                    activity.Confirm(() => i);
                }
            }

            log.Last()
               .ToString()
               .Should()
               .Match(@"*Confirmed = { 100 (@1*ms) }*");
        }

        [Test]
        public async Task Activity_confirmations_are_verifiable_from_the_LogEntry()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                log.Single().Confirmations.Count().Should().Be(0);

                activity.Confirm();

                log.Single().Confirmations.Count().Should().Be(0, "the log has not been written yet");
            }

            log.Last().Confirmations.Count().Should().Be(1);
        }
    }
}