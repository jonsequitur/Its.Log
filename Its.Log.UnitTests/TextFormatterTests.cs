// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [NUnit.Framework.Ignore("Scenario not finished")]
    [Category("Incubation")]
    [TestFixture]
    public class TextFormatterTests
    {
        [SetUp]
        public void SetUp()
        {
            Log.UnsubscribeAllFromEntryPosted();
            LogFormatter.ResetToDefault();
            Extension.EnableAll();
        }

        [TearDown]
        public void TearDown()
        {
            LogFormatter.TextFormatter = new SingleLineTextFormatter();    
        }

        private void WriteSomething()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            // again, isolating just the parts that are specific to boundary logging:
            LogFormatter<LogEntry>.RegisterForMembers(
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
        public void MultiLine()
        {
            LogFormatter.TextFormatter = new MultiLineTextFormatter();
            WriteSomething();
            // TODO (SingleLineTextFormatter) write test
            Assert.Fail("Test not written yet.");
        }


    }
}