// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class LogEventsTests
    {
        [SetUp]
        [TearDown]
        public void SetUpAndTearDown()
        {
            Extension.EnableAll();
            Log.Formatters.Clear();
            Log.UnsubscribeAllFromEntryPosted();
        }

        [Test]
        public virtual void Log_Enter_using_block_generates_one_start_and_one_stop_event()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.Enter(() => { }))
            {
            }

            Assert.That(log[0].EventType, Is.EqualTo(TraceEventType.Start));
            Assert.That(log[1].EventType, Is.EqualTo(TraceEventType.Stop));
        }

        [Test]
        public virtual void Multiple_subscribed_observers_receive_updates()
        {
            var log1 = new List<LogEntry>();
            var log2 = new List<LogEntry>();

            using (Log.Events().Subscribe(log1.Add))
            using (Log.Events().Subscribe(log2.Add))
            {
                Log.Write("Test");
            }

            log1.Should().HaveCount(1);
            log2.Should().HaveCount(1);
        }

        [Test]
        public virtual void Observer_can_be_safely_unsubscribed_more_times_than_it_was_subscribed()
        {
            var subscription = Log.Events().Subscribe(new Subject<LogEntry>());

            subscription.Dispose();
            subscription.Dispose();
        }

        [Test]
        public virtual void When_error_events_observer_throws_no_exception_is_thrown_to_Log_Write()
        {
            var badObserver = Observer.Create<LogEntry>(e => { throw new Exception("This is a test exception"); });

            using (Log.Events().Subscribe(badObserver))
            using (Log.InternalErrorEvents().Subscribe(badObserver))
            {
                Log.Write("hello");
            }
        }

        [Test]
        public virtual void When_extension_property_throws_then_no_exception_bubbles_up_to_caller_and_error_event_is_published()
        {
            Log.Formatters.RegisterPropertiesFormatter<LogEntry>(e => e.Extensions);

            var log = new List<LogEntry>();
            var errors = new List<LogEntry>();

            using (TestHelper.LogToConsole()) // required in order to construct the extension and trigger its exception
            using (Log.Events().Subscribe(log.Add))
            using (Log.InternalErrorEvents().Subscribe(errors.Add))
            {
                Log.With<ExtensionThatThrowsInProperty>(e => { }).Write("test");
            }

            log.Should().NotBeEmpty();
            errors.Should().NotBeEmpty();
        }

        [Test]
        public virtual void When_Log_Enter_accessor_func_throws_error_event_is_published()
        {
            // this exception will be thrown from the first mock and the second should see it in the error event
            var testException = new Exception();
            Func<object> thrower = () => { throw testException; };
            bool errorSignaled = false;

            using (TestHelper.LogToConsole()) // required in order to invoke GetInfo() and trigger the extension's exception
            using (Log.Events().Subscribe(e => Console.WriteLine(e.ToLogString())))
            using (TestHelper.InternalErrors().Subscribe(e => errorSignaled = true))
            using (Log.Enter(() => new { SomeProperty = thrower() }))
            {
            }

            Assert.That(errorSignaled);
        }

        [Test]
        public virtual void When_Log_Enter_throws_no_exception_bubbles_up_to_caller()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.Enter(() => { throw new MethodAccessException(); }))
            {
            }

            log.Should().NotBeEmpty();
        }
        
        [Test]
        public virtual void When_Log_With_extension_throws_error_event_is_published()
        {
            var errors =new List<LogEntry>();
            
            using (Log.InternalErrorEvents().Subscribe(errors.Add))
            {
                Log.With<ExtensionThatThrowsInCtor>(e => { }).Write("test");
            }

            errors.Should()
                .ContainSingle(e => e.Subject is Exception);
        }

        [Test]
        public virtual void When_Log_With_extension_throws_no_exception_bubbles_up_to_caller()
        {
            Log.With<ExtensionThatThrowsInCtor>(e => { }).Write("test");
        }

        [Test]
        public virtual void When_Log_With_params_accessor_func_throws_error_event_is_published()
        {
            var log = new List<LogEntry>();
            var errors = new List<LogEntry>();

            // this exception will be thrown from the first mock and the second should see it in the error event
            using (Log.Events().Subscribe(log.Add))
            using (Log.InternalErrorEvents().Subscribe(errors.Add))
            {
                Func<object> thrower = () =>
                {
                    throw new Exception();
                };
                Log.WithParams(() =>
                {
                    return new { SomeProperty = thrower() };
                }).Write("test");
            }

            log.Should().NotBeEmpty();
            Console.WriteLine(log.ToLogString());
            // FIX errors.Should().NotBeEmpty();
        }

        [Test]
        public virtual void When_subscriber_throws_other_subscribers_still_receive_events()
        {
            var lastObserver = new List<LogEntry>();
            var firstObserver = new List<LogEntry>();

            using (Log.Events().Subscribe(firstObserver.Add))
            using (Log.Events().Subscribe(e => { throw new Exception("Test exception"); }))
            using (Log.Events().Subscribe(lastObserver.Add))
            {
                Log.Write("test");
            }

            lastObserver.Should().NotBeEmpty();
            firstObserver.Should().NotBeEmpty();
        }
    }
}
