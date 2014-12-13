// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;
using Moq;
using Assert = NUnit.Framework.Assert;

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
            var observer1 = new Mock<IObserver<LogEntry>>();
            var observer2 = new Mock<IObserver<LogEntry>>();

            using (observer1.Object.SubscribeToLogEvents())
            using (observer2.Object.SubscribeToLogEvents())
            {
                Log.Write("Test");
            }

            observer1.Verify(
                o => o.OnNext(It.IsAny<LogEntry>()),
                Times.Once());
            observer2.Verify(
                o => o.OnNext(It.IsAny<LogEntry>()),
                Times.Once());
        }

        [Test]
        public virtual void Observer_can_be_safely_unsubscribed_more_times_than_it_was_subscribed()
        {
            var observer = new Mock<IObserver<LogEntry>>();

            var subscription = observer.Object.SubscribeToLogEvents();

            subscription.Dispose();
            subscription.Dispose();
        }

        [Test]
        public virtual void Subscribed_observer_receives_updates()
        {
            var observer = new Mock<IObserver<LogEntry>>();

            using (observer.Object.SubscribeToLogEvents())
            {
                Log.Write("test");
            }

            observer.Verify(
                o => o.OnNext(It.IsAny<LogEntry>()),
                Times.Once());
        }

        [Test]
        public virtual void When_error_events_observer_throws_no_exception_is_thrown_to_Log_Write()
        {
            var badObserver = new Mock<IObserver<LogEntry>>();
            badObserver
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()))
                .Throws(new Exception("This is a test exception"));

            using (badObserver.Object.SubscribeToLogEvents())
            using (badObserver.Object.SubscribeToLogInternalErrors())
            {
                Log.Write("hello");
            }
        }

        [Test]
        public virtual void When_extension_property_throws_then_no_exception_bubbles_up_to_caller_and_error_event_is_published()
        {
            Log.Formatters.RegisterPropertiesFormatter<LogEntry>(e => e.Extensions);
            var logObserver = new Mock<IObserver<LogEntry>>();
            logObserver
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()));
            var errorObserver = new Mock<IObserver<LogEntry>>();
            errorObserver
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()));

            using (TestHelper.LogToConsole()) // required in order to construct the extension and trigger its exception
            using (logObserver.Object.SubscribeToLogEvents())
            using (errorObserver.Object.SubscribeToLogInternalErrors())
            {
                Log.With<ExtensionThatThrowsInProperty>(e => { }).Write("test");
            }

            logObserver.VerifyAll();
            errorObserver.Verify(o => o.OnNext(It.IsAny<LogEntry>()));
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
            {
                using (Log.Enter(() => new { SomeProperty = thrower() }))
                {
                }
            }

            Assert.That(errorSignaled);
        }

        [Test]
        public virtual void When_Log_Enter_throws_no_exception_bubbles_up_to_caller()
        {
            var logObserver = new Mock<IObserver<LogEntry>>();
            logObserver
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()));
            using (logObserver.Object.SubscribeToLogEvents())
            using (Log.Enter(() => { throw new MethodAccessException(); }))
            {
            }

            logObserver.VerifyAll();
        }

        [Test]
        public virtual void When_Log_With_extension_throws_error_event_is_published()
        {
            var errorObserver = new Mock<IObserver<LogEntry>>();
            errorObserver
                .Setup(o => o.OnNext(It.Is<LogEntry>((LogEntry e) => e.Subject is Exception)));
            using (errorObserver.Object.SubscribeToLogInternalErrors())
            {
                Log.With<ExtensionThatThrowsInCtor>(e => { }).Write("test");
            }

            errorObserver.VerifyAll();
        }

        [Test]
        public virtual void When_Log_With_extension_throws_no_exception_bubbles_up_to_caller()
        {
            Log.With<ExtensionThatThrowsInCtor>(e => { }).Write("test");
        }

        [Test]
        public virtual void When_Log_With_params_accessor_func_throws_error_event_is_published()
        {
            // this exception will be thrown from the first mock and the second should see it in the error event
            var testException = new Exception();

            // subscribe to log events
            var logObserver = new Mock<IObserver<LogEntry>>();
            logObserver
                .Setup(e => e.OnNext(It.IsAny<LogEntry>()));

            // subscribe to error events
            var errorObserver = new Mock<IObserver<LogEntry>>();
            errorObserver
                .Setup(o => o.OnNext(It.Is<LogEntry>(e => e.Subject is Exception)));

            using (logObserver.Object.SubscribeToLogEvents())
            using (errorObserver.Object.SubscribeToLogInternalErrors())
            {
                Func<object> thrower = () => { throw testException; };
                Log.WithParams(() => new { SomeProperty = thrower() }).Write("test");
            }

            logObserver.VerifyAll();
        }

        [Test]
        public virtual void When_subscriber_throws_other_subscribers_still_receive_events()
        {
            var lastObserver = new Mock<IObserver<LogEntry>>();
            lastObserver
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()));
            var firstObserver = new Mock<IObserver<LogEntry>>();
            firstObserver
                .Setup(o => o.OnNext(It.IsAny<LogEntry>()));

            using (Log.Events().Subscribe(firstObserver.Object))
            using (Log.Events().Subscribe(e => { throw new Exception("Test exception"); }))
            using (Log.Events().Subscribe(lastObserver.Object))
            {
                Log.Write("test");
            }

            lastObserver.VerifyAll();
        }
    }
}