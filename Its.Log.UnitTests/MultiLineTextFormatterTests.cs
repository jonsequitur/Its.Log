// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [Category("Incubation")]
    [TestFixture]
    public class MultiLineTextFormatterTests
    {
        private string log = "";
        private IDisposable subscription;

        [SetUp]
        public void SetUp()
        {
            Log.UnsubscribeAllFromEntryPosted();
            Formatter.ResetToDefault();
            Extension.EnableAll();
            Formatter.TextFormatter = new MultiLineTextFormatter();
            MultiLineTextFormatter.DebugMode = false;

            log = "";
            subscription = Log.Events().Subscribe(e => log += e.ToLogString());
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine(log);
            Formatter.TextFormatter = new SingleLineTextFormatter();
            subscription.Dispose();
        }

        [Test]
        public void Log_entry_starts_unindented()
        {
            Log.Write(() => "hello");

            Assert.That(log,
                        Is.StringStarting("Information"));
        }

        [Test]
        public void Subsequent_log_entry_starts_unindented()
        {
            Log.Write(() => "hello");
            using (Log.Enter(() => new { one = 1, two = 2, three = 3 }))
            {
            }

            Assert.That(log,
                        Contains.Substring("\nStart"));
            Assert.That(log,
                        Contains.Substring("\nStop"));
        }

        [Test]
        public void Properties_are_indented_on_new_lines()
        {
            Log.Write(() => "hi");

            Assert.That(log,
                        Contains.Substring("\n\tCallingType"));
            Assert.That(log,
                        Contains.Substring("\n\tMessage"));
        }

        [Test]
        public void Params_sequence_is_indented()
        {
            Log.WithParams(() => new { one = 1, two = 2, three = 3 }).Write("hello");

            Assert.That(log, Contains.Substring("\n\t\tone: 1"));
            Assert.That(log, Contains.Substring("\n\t\ttwo: 2"));
            Assert.That(log, Contains.Substring("\n\t\tthree: 3"));
        }
        
        [Test]
        public void Primitive_sequence_is_indented()
        {
            MultiLineTextFormatter.DebugMode = true;
            Log.Write(() => new [] { 1, 2, 3 });

            Assert.That(log, Contains.Substring("\n\t\t1"));
            Assert.That(log, Contains.Substring("\n\t\t2"));
            Assert.That(log, Contains.Substring("\n\t\t3"));
        }
    }
}