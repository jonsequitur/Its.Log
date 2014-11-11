using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Its.Recipes;
using Its.Log.Instrumentation;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using TraceListener = Its.Log.Instrumentation.TraceListener;

namespace Its.Log.Lite.UnitTests
{
    [TestFixture]
    public class When_ItsLogLite_is_used_with_ItsLog
    {
        private AnonymousDisposable cleanup = null;
        private StringBuilder traceOutput;

        [SetUp]
        public void SetUp()
        {
            traceOutput = new StringBuilder();
            var itsLogListener = new TraceListener();
            Trace.Listeners.Add(itsLogListener);
            EventHandler<InstrumentationEventArgs> handler = (sender, args) => traceOutput.AppendLine(args.LogEntry.ToString());
            Instrumentation.Log.EntryPosted += handler;

            Log.ResetItsLogCheck();

            cleanup = new AnonymousDisposable(() =>
            {
                Instrumentation.Log.EntryPosted -= handler;
                Trace.Listeners.Remove(itsLogListener);
            });
        }

        [TearDown]
        public void TearDown()
        {
            cleanup.Dispose();
        }

        [Test]
        public void lambdas_passed_to_Write_are_executed_and_written_to_Trace()
        {
            var words = Any.Paragraph(4);

            Log.Write(() => words);

            traceOutput.ToString().Should().Contain(words);
        }

        [Test]
        public void comments_passed_to_Write_are_written_to_Trace()
        {
            var words = Any.Paragraph(4);

            Log.Write(() => "", words);

            traceOutput.ToString().Should().Contain(words);
        }

        [Test]
        public void method_names_are_captured_by_Write()
        {
            Log.Write(() => "hello");
            traceOutput.ToString().Should().Contain(MethodBase.GetCurrentMethod().Name);
        }

        [Test]
        public void method_names_are_captured_by_Enter()
        {
            Log.Enter(() => "");

            traceOutput.ToString().Should().Contain(MethodBase.GetCurrentMethod().Name);
        }

        [Test]
        public void method_names_are_captured_by_Exit()
        {
            var activity = Log.Enter(() => "");

            traceOutput.Clear();

            activity.Dispose();

            traceOutput.ToString().Should().Contain(MethodBase.GetCurrentMethod().Name);
        }

        [Test]
        public void when_an_exception_is_logged_then_the_LogEntry_Subject_is_the_exception()
        {
            var entries = new List<LogEntry>();

            using (Instrumentation.Log.Events().Subscribe(entries.Add))
            {
                Log.Write(() => new Exception("oops!"));
            }

            entries.Single().Subject.Should().BeOfType<Exception>();
        }

        [NUnit.Framework.Ignore("work in progress")]
        [Test]
        public void when_ItsLogLite_gets_called_first_then_an_exception_triggers_a_recheck_for_Its_Log()
        {
            Trace.Listeners.Clear();

            

            // FIX (but_ItsLogLite_gets_called_first_then_an_exception_triggers_a_recheck_for_Its_Log) write test
            Assert.Fail("Test not written yet.");
        }
    }
}