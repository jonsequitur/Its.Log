// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class ExceptionTrackingTests
    {
        [Test]
        public void Exception_is_not_retagged_with_id()
        {
            var ex = new NullReferenceException();

            Assert.IsFalse(ex.Data.Contains("__Its_Log_ExceptionID__"));

            // throw the exception twice, make sure the unique id is only added once
            var count = ex.Data.Count;

            Log.Write(ex);

            Assert.IsTrue(ex.Data.Contains("__Its_Log_ExceptionID__"));

            Log.Write(ex);

            Assert.AreEqual(count + 1, ex.Data.Count);
        }

        [Test]
        public void Exception_id_propagates_from_exception_to_entry()
        {
            var uniqueID = Guid.NewGuid();

            var ex = new Exception("This is a test", new NullReferenceException());
            ex.InnerException.Data.Add("__Its_Log_ExceptionID__", uniqueID);

            var entry = new LogEntry(ex);
            Log.Write(entry);
            Assert.AreEqual(uniqueID, entry.ExceptionId);
        }

        [Test]
        public virtual void Exception_id_propagates_from_log_entry_to_exception()
        {
            var ex = new Exception();
            var entry = new LogEntry(ex);
            Log.Write(entry);
            Assert.AreNotEqual(Guid.Empty, entry.ExceptionId);
            Assert.AreEqual(entry.ExceptionId, (Guid) ex.Data["__Its_Log_ExceptionID__"]);
        }

        [Test]
        public void Exception_id_Exception_Data_entry_does_not_appear_in_formatted_exception_string()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.Events().LogToConsole())
            {
                try
                {
                    throw new InvalidOperationException();
                }
                catch (Exception ex)
                {
                    Log.Write(() => ex);
                }
            }

            Assert.That(
                log.Single().ToLogString(),
                Is.Not.StringContaining("__Its_Log_ExceptionID__"));
        } 
    }
}