using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using StringAssert = NUnit.Framework.StringAssert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class ExceptionExtensionsTests
    {
        [TearDown]
        public void TearDown()
        {
            Log.CanThrowWhen = null;
        }

        [Test]
        public void AggregateException_InnerExceptions_are_included()
        {
            var aggregateWithOneInner = new AggregateException("oops", new Exception());
            Assert.That(
                aggregateWithOneInner.InnerExceptions().Count(),
                Is.EqualTo(1));

            var aggregateWithSeveralInners = new AggregateException(new Exception(), new ExecutionEngineException(), new InvalidOperationException());
            Assert.That(
                aggregateWithSeveralInners.InnerExceptions().Count(),
                Is.EqualTo(3));
        }

        [Test]
        public void Exception_can_be_marked_as_handled()
        {
            var exception = new Exception();

            Assert.That(!exception.HasBeenHandled());

            exception.MarkAsHandled();

            Assert.That(exception.HasBeenHandled());
        }

        [Test]
        public void Exception_WithData_anonymous_type_object_is_added_as_string()
        {
            var writer = new StringWriter();
            var writerString = "This is writer.ToString()";
            writer.Write(writerString);
            var string42 = "42";
            int fortyone = 41;
            string stringNull = null;

            var exception = new Exception();
            exception.WithData(new { fortyone = fortyone, string42, stringNull, writer.Encoding, writerString = writer.ToString() });

            Assert.That(exception.Data.Count, Is.EqualTo(1));

            var exceptionData = exception.Data["ItsLog_ExceptionData_"].ToString();
            StringAssert.Contains(string.Format("fortyone = {0}", fortyone), exceptionData);
            StringAssert.Contains(string.Format("string42 = {0}", string42), exceptionData);
            StringAssert.Contains("stringNull = [null]", exceptionData);
            StringAssert.Contains(string.Format("writerString = {0}", writerString), exceptionData);
        }

        [Test]
        public void Exception_WithData_two_anonymous_type_objects_are_added_with_two_keys()
        {
            int fortyone = 41;
            var string42 = "42";

            var exception = new Exception();
            exception.WithData(new { fortyone = fortyone }).WithData(new { string42 });

            Assert.That(exception.Data.Count, Is.EqualTo(2));

            var exceptionData = exception.Data["ItsLog_ExceptionData_"].ToString();
            StringAssert.Contains(string.Format("fortyone = {0}", fortyone), exceptionData);

            exceptionData = exception.Data["ItsLog_ExceptionData_2"].ToString();
            StringAssert.Contains(string.Format("string42 = {0}", string42), exceptionData);
        }

        [Test]
        public void Log_Write_can_be_prevented_from_swallowing_exceptions_from_input_lambda()
        {
            Log.CanThrowWhen = ex => true;

            Assert.Throws<InvalidCastException>(() => Log.Write(() =>
            {
                throw new InvalidCastException();
                return "hi";
            }));
        }

        [Test]
        public void Log_Enter_can_be_prevented_from_swallowing_exceptions_from_input_lambda()
        {
            Log.CanThrowWhen = ex => true;

            Assert.Throws<InvalidCastException>(() => Log.Enter(() =>
            {
                throw new InvalidCastException();
                return "hi";
            }));
        }

        [Test]
        public void Log_Enter_can_be_prevented_from_swallowing_exceptions_from_event_subscriber()
        {
            Log.CanThrowWhen = ex => true;

            using (Log.Events().Subscribe(e => { throw new InvalidCastException(); }))
            {
                Assert.Throws<InvalidCastException>(() => Log.Enter(() => "hi"));
            }
        }

        [Test]
        public void When_an_exception_is_swallowed_the_stack_trace_of_its_callers_can_be_captured_using_WithFullStackTrace()
        {
            var log = new List<LogEntry>();
            using (Log.Events().Subscribe(log.Add))
            {
                CatchAndSwallowExceptionWithFullStackLog();
            }

            Assert.That(
                log.Single().ToLogString(),
                Is.StringContaining(MethodBase.GetCurrentMethod().Name));
        }

        [Test]
        public void When_an_exception_is_augmented_with_WithFullStackTrace_then_exception_stack_trace_is_included_in_augmented_stack_trace()
        {
            var log = new List<LogEntry>();
            using (Log.Events().Subscribe(log.Add))
            using (Log.Events().LogToConsole())
            {
                CatchAndSwallowExceptionWithFullStackLog();
            }

            // TODO: (When_an_exception_is_augmented_with_WithFullStackTrace_then_exception_stack_trace_is_included_in_augmented_stack_trace) the stack trace is a little weird... maybe try to remove the Its.Log frames?
            Assert.That(
                log.Single().ToLogString(),
                Is.StringContaining("at Its.Log.Instrumentation.UnitTests.ExceptionExtensionsTests.ThrowException"));
        }

        [Test]
        public void When_an_exception_is_augmented_with_WithFullStackTrace_then_the_Exception_Data_entry_is_not_shown_in_standard_output()
        {
            var log = new List<LogEntry>();
            using (Log.Events().Subscribe(log.Add))
            {
                CatchAndSwallowExceptionWithFullStackLog();
            }

            Assert.That(
                log.Single().ToLogString(),
                Is.Not.StringContaining(ExceptionExtensions.FullStackTraceKey));
        }

        private static void CatchAndSwallowExceptionWithFullStackLog()
        {
            try
            {
                ThrowException();
            }
            catch (Exception ex)
            {
                Log.Write(() => ex.WithFullStackTrace());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowException()
        {
            throw new ArgumentException();
        }
    }
}