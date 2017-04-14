// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class TraceListenerTests
    {
        [SetUp]
        public void SetUp()
        {
            Trace.Listeners.Add(new TraceListener());
        }

        [TearDown]
        public void TearDown()
        {
            Trace.Listeners.Clear();
        }

        /// <summary>
        ///   Tests that LogEntry.Category is set correctly on calls to Log.Write
        /// </summary>
        [Test]
        public void Category_is_set_correctly_for_trace_write_overload_string_string()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Trace.Write("test", "the category");
            }

            log.Should().ContainSingle(e => e.Category =="the category");
        }

        /// <summary>
        ///   Tests that LogEntry.Category is set correctly on calls to Log.Write
        /// </summary>
        [Test]
        public void Category_is_set_correctly_for_trace_write_overload_object_string()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Trace.Write(new object(), "the category");
            }

            log.Should().ContainSingle(e => e.Category == "the category");
        }

        /// <summary>
        ///   Tests that LogEntry.Category is set correctly on calls to Trace.WriteLine
        /// </summary>
        [Test]
        public void Category_is_set_correctly_for_trace_write_line_overload_string_string()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Trace.WriteLine("test", "the category");
            }

            log.Should().ContainSingle(e => e.Category == "the category");
        }

        /// <summary>
        ///   Tests that LogEntry.Category is set correctly on calls to Trace.WriteLine
        /// </summary>
        [Test]
        public void Category_is_set_correctly_for_trace_write_line_overload_object_string()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Trace.WriteLine(new object(), "the category");
            }

            log.Should().ContainSingle(e => e.Category == "the category");
        }

        [Test]
        public void Passing_Trace_Write_a_Func_logs_result_of_Func()
        {
            Func<ArgumentException> func = () => new ArgumentException("some_really_important_arg");
            string log = "";

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Trace.Write(func);
            }

            Assert.That(log, Contains.Substring("some_really_important_arg"));
        }

        [Test]
        public void Passing_Trace_Write_a_Func_provides_CallingType_and_CallingMethod()
        {
            Func<ArgumentException> func = () => new ArgumentException("some_really_important_arg");
            string log = "";

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Trace.Write(func);
            }

            Assert.That(log, Contains.Substring("Passing_Trace_Write_a_Func_provides_CallingType_and_CallingMethod"));
            Assert.That(log, Contains.Substring(GetType().Name));
        }

        [Test]
        public void Passing_Trace_Write_with_category_overload_a_Func_logs_result_of_Func_as_well_as_category()
        {
            Func<ArgumentException> func = () => new ArgumentException("some_really_important_arg");
            string log = "";

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Trace.Write(func, "some category");
            }

            Assert.That(log, Contains.Substring("some_really_important_arg"));
            Assert.That(log, Contains.Substring("some category"));
        }

        [Test]
        public void Passing_Trace_Write_with_category_overload_a_Func_provides_CallingType_and_CallingMethod_as_well_as_category()
        {
            Func<ArgumentException> func = () => new ArgumentException("some_really_important_arg");
            string log = "";

            using (TestHelper.LogToConsole())
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Trace.Write(func, "some category");
            }

            Assert.That(log, Contains.Substring("Passing_Trace_Write_with_category_overload_a_Func_provides_CallingType_and_CallingMethod_as_well_as_category"));
            Assert.That(log, Contains.Substring(GetType().Name));
            Assert.That(log, Contains.Substring("some category"));
        }

        [Test]
        public void When_Log_output_is_sent_to_trace_and_ItsLog_TraceListener_is_running_then_TraceListener_prevents_recursion()
        {
            string log = "";
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            using (Log.Events().Subscribe(e => Trace.WriteLine(e.ToLogString())))
            {
                Log.Write(() => "oh hai");
            }

            Assert.That(log, Contains.Substring("When_Log_output_is_sent_to_trace_and_ItsLog_TraceListener_is_running_then_TraceListener_prevents_recursion"));
            Assert.That(log, Contains.Substring("oh hai"));
            Assert.That(log, Contains.Substring(GetType().Name));
        }
    }
}