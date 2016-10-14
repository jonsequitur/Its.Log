// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using FluentAssertions;
using Its.Recipes;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Lite.UnitTests
{
    [TestFixture]
    public class When_ItsLogLite_is_used_without_ItsLog
    {
        private AnonymousDisposable cleanup = null;
        private StringBuilder traceOutput;

        [SetUp]
        public void SetUp()
        {
            // make sure the Its.Log TraceListener is not registered
            foreach (TraceListener listener in Trace.Listeners)
            {
                if (listener is Instrumentation.TraceListener)
                {
                    Trace.Listeners.Remove(listener);
                    break;
                }
            }
            Log.ResetItsLogCheck();

            traceOutput = new StringBuilder();
            var writer = new StringWriter(traceOutput);
            var textWriterTraceListener = new TextWriterTraceListener(writer);
            Trace.Listeners.Add(textWriterTraceListener);

            cleanup = new AnonymousDisposable(() => Trace.Listeners.Remove(textWriterTraceListener));
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

        [NUnit.Framework.Ignore("Test not finished")]
        [Test]
        public void the_test_category_is_set_to_the_assembly_name_and_can_be_used_for_filtering()
        {
            using (new TestTraceListener())
            {
                Log.Write(() => "message", "comment");
            }

            // TODO (the_test_category_is_set_to_the_assembly_name) write test
            Assert.Fail("Test not written yet.");
        }

        public class TestTraceListener : TraceListener
        {
            public TestTraceListener()
            {
                Trace.Listeners.Add(this);
            }

            public override void Write(string message)
            {
                Console.WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                Console.WriteLine(message);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Trace.Listeners.Remove(this);
                }
                base.Dispose(disposing);
            }
        }
    }
}