// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class AsyncTests : IDisposable
    {
        public void Dispose()
        {
            Log.UnsubscribeAllFromEntryPosted();
        }

        [Test]
        public void LogActivity_Trace_captures_current_method_name()
        {
            var log = new List<LogEntry>();

            var helper = new AsyncTestHelper();

            using (var activity = Log.Enter(() => { }))
            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(log.Add))
            {
                var result = helper.BeginSomething(
                    _ => activity.Complete(() => { }),
                    activity);
                helper.EndSomething(result);
            }

            log.Select(e => e.CallingMethod)
               .Should()
               .BeEquivalentTo("BeginSomething",
                               "EndSomething", 
                               nameof(LogActivity_Trace_captures_current_method_name));
        }

        [Test]
        public void LogActivity_Complete_writes_current_method_name()
        {
            var log = new List<LogEntry>();
            var methodName = MethodBase.GetCurrentMethod().Name;
            var helper = new AsyncTestHelper();

            using (var activity = Log.Enter(() => { }))
            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(log.Add))
            {
                var result = helper.BeginSomething(
                    _ => activity.Complete(() => { }),
                    activity);
                helper.EndSomething(result);
            }

            log.Should()
               .ContainSingle(e => e.CallingMethod == methodName &&
                                   e.EventType == TraceEventType.Stop);
        }

        [Test]
        public async Task Log_Write_in_an_async_method_captures_the_current_method_name()
        {
            var events = new List<LogEntry>();

            using (Log.Events().Subscribe(events.Add))
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
            if (activity != null)
            {
                activity.Trace(() => "hello");
            }

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
                if (state.Item2 != null)
                {
                    state.Item2.Trace(() => "hello");
                }
                if (state.Item1 != null)
                {
                    state.Item1.Invoke(result);
                }
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
