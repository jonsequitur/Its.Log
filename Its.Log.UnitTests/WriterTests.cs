// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [NUnit.Framework.Ignore("Scenario not finished")]
    [Category("Incubation")]
    [TestFixture]
    public class WriterTests
    {
        [SetUp]
        public void SetUp()
        {
            Log.UnsubscribeAllFromEntryPosted();
            Formatter.ResetToDefault();
            Extension.EnableAll();
        }

        private void WriteSomething()
        {
             Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            // again, isolating just the parts that are specific to boundary logging:
            Log.Formatters.RegisterPropertiesFormatter<LogEntry>(
                e => e.Message,
                e => e.Params,
                e => e.ElapsedMilliseconds);

            var birthdays = new List<DateTime>();
            using (var activity = Log.Enter(() => new { birthdays }))
            {
                birthdays.Add(Person.MarkHamill.DateOfBirth);
                activity.Trace("adding a birthday...");
                birthdays.Add(Person.CarrieFisher.DateOfBirth);
            }
        }

        [Test]
        public void SingleLineWriter()
        {
            Formatter.CreateWriter = () => new SingleLineWriter();
            WriteSomething();
            // TODO (SingleLineWriter) write test
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public void MultiLineWriter()
        {
            Formatter.CreateWriter = () => new MultiLineWriter();
            WriteSomething();
            // TODO (SingleLineWriter) write test
            Assert.Fail("Test not written yet.");
        }
    }

    internal class SingleLineWriter : StringWriter
    {
        public SingleLineWriter() : base(CultureInfo.InvariantCulture)
        {
            base.NewLine = Formatter.ClosingDelimiter;
        }
    }
    
    internal class MultiLineWriter : StringWriter
    {
        public MultiLineWriter() : base(CultureInfo.InvariantCulture)
        {
            base.NewLine = Environment.NewLine;
        }
    }
}