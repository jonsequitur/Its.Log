// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class AsyncTests
    {
        [TearDown]
        public void TearDown()
        {
            Log.UnsubscribeAllFromEntryPosted();
        }

        [Test]
        public void LogActivity_Trace_captures_current_method_name()
        {
            var observer = new Mock<IObserver<LogEntry>>();
            observer.Setup(o => o.OnNext(
                It.Is<LogEntry>(
                    e => e.CallingMethod.Contains("BeginSomething"))));
            observer.Setup(o => o.OnNext(
                It.Is<LogEntry>(
                    e => e.CallingMethod.Contains("EndSomething"))));

            var helper = new AsyncTestHelper();

            using (var activity = Log.Enter(() => { }))
            using (TestHelper.LogToConsole())
            using (observer.Object.SubscribeToLogEvents())
            {
                var result = helper.BeginSomething(
                    _ => activity.Complete(() => { }),
                    activity);
                helper.EndSomething(result);
            }

            observer.VerifyAll();
        }

        [Test]
        public void LogActivity_Complete_writes_current_method_name()
        {
            var methodName = MethodBase.GetCurrentMethod().Name;
            var observer = new Mock<IObserver<LogEntry>>();
            observer
                .Setup(
                    o => o.OnNext(
                        It.Is<LogEntry>(
                            e => e.CallingMethod == methodName &&
                                 e.EventType == TraceEventType.Stop)));

            var helper = new AsyncTestHelper();

            using (var activity = Log.Enter(() => { }))
            using (TestHelper.LogToConsole())
            using (observer.Object.SubscribeToLogEvents())
            {
                var result = helper.BeginSomething(
                    _ => activity.Complete(() => { }),
                    activity);
                helper.EndSomething(result);
            }

            observer.VerifyAll();
        }

        [Test]
        public async Task Log_Write_in_an_async_method_captures_the_current_method_name()
        {
            var events = new List<LogEntry>();

            using(Log.Events().Subscribe(events.Add))
            {
                await new AsyncTestHelper().DoSomethingAsync();
            }

            events.Single().CallingMethod.Should().Be("DoSomethingAsync");
        }
    }

    public class AsyncTestHelper
    {
        public IAsyncResult BeginSomething(AsyncCallback callback, object state)
        {
            var activity = state as ILogActivity;
            activity?.Trace(() => "hello");

            return new AsyncResult
                       {
                           AsyncState = new Tuple<AsyncCallback, ILogActivity>(callback, state as ILogActivity)
                       };
        }

        public void EndSomething(IAsyncResult result)
        {
            var state = result.AsyncState as Tuple<AsyncCallback, ILogActivity>;
            if (state != null)
            {
                state.Item2?.Trace(() => "hello");
                state.Item1?.Invoke(result);
            }
        }

        public async Task DoSomethingAsync()
        {
            Log.Write(() => "hello");
        }

        private class AsyncResult : IAsyncResult
        {
            public bool IsCompleted { get; set; }
            public WaitHandle AsyncWaitHandle { get; private set; }
            public object AsyncState { get; set; }
            public bool CompletedSynchronously { get; private set; }
        }
    }
}