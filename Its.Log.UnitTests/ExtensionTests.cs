// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    /// <summary>
    /// Unit tests for ExtensionTests
    /// </summary>
    [TestFixture]
    public class ExtensionTests
    {
        [SetUp]
        public void SetUp()
        {
            Extension.EnableAll();
        }

        [TearDown]
        public void CleanUp()
        {
            Log.UnsubscribeAllFromEntryPosted();
        }

        public enum EntryType
        {
            Information,
            Error
        }

        [Test]
        public virtual void EnableAll_enables_extensions_that_were_disabled_individually()
        {
            Extension<Params>.Disable();
            Assert.IsFalse(Extension<Params>.IsEnabled);

            Extension.EnableAll();

            Assert.IsTrue(Extension<Params>.IsEnabled);
        }

        [Test]
        public virtual void Exceptions_thrown_in_extension_anonymous_delegates_are_published_to_error_events()
        {
            var log = new List<LogEntry>();

            using (Log.InternalErrorEvents().Subscribe(log.Add))
            {
                Log
                    .With<EventLogInfo>(el => el.EventId = el.EventId / (el.EventId - el.EventId))
                    .Write("here i am");
            }

            log.Should()
               .ContainSingle(e => e.Subject is DivideByZeroException);
        }

        [Test]
        public virtual void Exceptions_thrown_in_extension_anonymous_delegates_do_not_bubble_up()
        {
            Log
                .With<EventLogInfo>(el => el.EventId = el.EventId / (el.EventId - el.EventId))
                .Write("here i am");
        }

        [Test]
        public virtual void Extensions_can_be_disabled_globally_separately()
        {
            Extension<EventLogInfo>.Disable();
            Extension<Params>.Enable();

            Assert.IsTrue(Extension<Params>.IsEnabled);
            Assert.IsFalse(Extension<EventLogInfo>.IsEnabled);
        }

        [Ignore("Not supported")]
        [Test]
        public virtual void When_same_extension_type_is_added_more_than_once_all_instances_are_logged()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Log
                    .With<EventLogInfo>(el => el.EventId = 11111)
                    .With<EventLogInfo>(el => el.EventId = 22222)
                    .Write("here i am");
            }

            log.Should()
               .ContainSingle(e => e.ToLogString().Contains("11111") &&
                                   e.ToLogString().Contains("22222"));
        }

        [Test]
        public virtual void Enter_With_with_overload_having_no_initialization_action_writes_extension_on_enter_and_exit()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.With<FooExtension>().Enter(() => { }))
            {
            }

            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Start &&
                                   e.HasExtension<FooExtension>());
            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Stop &&
                                   e.HasExtension<FooExtension>());
        }

        [Test]
        public virtual void Enter_and_Dispose_magic_barbell_overload_with_chained_extensions_writes_extension_info_twice()
        {
            var eventId = 424242;
            var log = new List<LogEntry>();

            using (Log.Events().LogToConsole())
            using (Log.Events().Subscribe(log.Add))
            using (Log
                .With<Counter>(c => c.Increment())
                .With<EventLogInfo>(ext => { ext.EventId = eventId; })
                .Enter(() => { }))
            {
            }

            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Start &&
                                   e.GetExtension<Counter>().Count == 1 &&
                                   e.GetExtension<EventLogInfo>().EventId == eventId);
            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Stop &&
                                   e.GetExtension<Counter>().Count == 1 &&
                                   e.GetExtension<EventLogInfo>().EventId == eventId);
        }

        [Test]
        public virtual void Enter_and_Dispose_params_overload_with_chained_extensions_writes_extension_info_twice()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log
                .With<Counter>(c => c.Increment())
                .With<EventLogInfo>(ext => ext.EventId = 123)
                .Enter(() => new { Foo = "foo" }))
            {
            }

            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Start &&
                                   e.GetExtension<Counter>().Count == 1 &&
                                   e.GetExtension<EventLogInfo>().EventId == 123);
            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Stop &&
                                   e.GetExtension<Counter>().Count == 1 &&
                                   e.GetExtension<EventLogInfo>().EventId == 123);
        }

        [Test]
        public virtual void Enter_and_Dispose_magic_barbell_overload_with_single_extension_writes_extension_info_twice()
        {
            var i = 3246362;
            var log = new List<LogEntry>();

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(log.Add))
            using (Log
                .With<EventLogInfo>(ext => ext.EventId = i)
                .Enter(() => { }))
            {
            }

            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Start &&
                                   e.GetExtension<EventLogInfo>().EventId == i);
            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Stop &&
                                   e.GetExtension<EventLogInfo>().EventId == i);
        }

        [Test]
        public virtual void Enter_and_Dispose_params_overload_with_single_extension_writes_extension_info_twice()
        {
            var i = 3246362;
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log
                .With<EventLogInfo>(ext => ext.EventId = i)
                .Enter(() => new { Foo = "foo" }))
            {
            }

            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Start &&
                                   e.GetExtension<EventLogInfo>().EventId == i);
            log.Should()
               .ContainSingle(e => e.EventType == TraceEventType.Stop &&
                                   e.GetExtension<EventLogInfo>().EventId == i);
        }

        [Test]
        public virtual void Write_with_single_extension_writes_extension_info()
        {
            var i = 3246362;
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (TestHelper.OnEntryPosted(Console.WriteLine))
            {
                Log
                    .With<EventLogInfo>(ext => ext.EventId = i)
                    .Write("SingleExtension");
            }

            log.Should()
               .ContainSingle(e => e.GetExtension<EventLogInfo>().EventId == i);
        }

        [Test]
        public void Params_usage_of_formatter_for_ToString_does_not_cause_stack_overflow()
        {
            var count = 0;
            var log = new List<LogEntry>();
            for (var i = 1; i <= 20; i++)
            {
                Formatter.RecursionLimit = i;
                using (TestHelper.LogToConsole())
                using (Log.Events().Subscribe(e => count++))
                {
                    Log.WithParams(() => new { Formatter.RecursionLimit }).Write("testing...");
                }
            }

            Assert.That(count, Is.EqualTo(20));
        }

        [Test]
        public void Extension_classes_can_specify_that_they_evaluate_only_on_enter()
        {
            var events = new List<LogEntry>();
            var methodName = MethodBase.GetCurrentMethod().Name;

            using (Log.Events().Subscribe(events.Add))
            using (Log.With<EnterCounter>().Enter(() => { }))
            {
                EnterCounter.Count(methodName).Should().Be(1);
            }

            EnterCounter.Count(methodName).Should().Be(1);
        }

        [Test]
        public void Extension_classes_can_specify_that_they_evaluate_only_on_exit()
        {
            var events = new List<LogEntry>();
            var methodName = MethodBase.GetCurrentMethod().Name;

            using (Log.Events().Subscribe(events.Add))
            using (Log.With<ExitCounter>().Enter(() => { }))
            {
                ExitCounter.Count(methodName).Should().Be(0);
            }

            ExitCounter.Count(methodName).Should().Be(1);
        }

        [Test]
        public async Task Extension_classes_can_specify_that_they_evaluate_on_enter_and_exit()
        {
            var i = 1;
            var events = new List<LogEntry>();

            using (Log.Events().Subscribe(events.Add))
            using (Log.With<CaptureResult>(c => c.Result = i).Enter(() => { }))
            {
                i = 2;

                events.Last().GetExtension<CaptureResult>().Result.Should().Be(1);
            }

            events.Last().GetExtension<CaptureResult>().Result.Should().Be(2);
        }
    }

    public class EnterCounter : IApplyOnEnter
    {
        private static readonly ConcurrentDictionary<string, int> count = new ConcurrentDictionary<string, int>();

        public static int Count(string methodName)
        {
            return count.GetOrAdd(methodName, _ => 0);
        }

        public void OnEnter(LogEntry logEntry)
        {
            count.AddOrUpdate(logEntry.CallingMethod, _ => 1, (_, c) => c + 1);
        }
    }

    public class ExitCounter : IApplyOnExit
    {
        private static readonly ConcurrentDictionary<string, int> count = new ConcurrentDictionary<string, int>();

        public static int Count(string methodName)
        {
            return count.GetOrAdd(methodName, _ => 0);
        }

        public void OnExit(LogEntry logEntry)
        {
            count.AddOrUpdate(logEntry.CallingMethod, _ => 1, (_, c) => c + 1);
        }
    }

    public class CaptureResult : IApplyOnExit, IApplyOnEnter
    {
        public object Result { get; set; }

        public void OnExit(LogEntry logEntry)
        {
        }

        public void OnEnter(LogEntry logEntry)
        {
        }
    }

    public class FooExtension
    {
        public string Foo = "bar";
    }
}
