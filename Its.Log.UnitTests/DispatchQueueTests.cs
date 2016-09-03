// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class DispatchQueueTests
    {
        [Test]
        public async Task log_entries_can_be_queued()
        {
            var log = new List<LogEntry>();
            using (new LogEntryDispatchQueue(send: async e => log.Add(e)))
            {
                WriteInParallel(10);
            }

            log.Should().HaveCount(10);
        }

        [Test]
        public async Task telemetry_events_can_be_queued()
        {
            var log = new List<Telemetry>();

            using (new TelemetryDispatchQueue(send: async e => log.Add(e)))
            {
                WriteInParallel(10, _ =>
                {
                    using (Log.With<Telemetry>().Enter(() => { }))
                    {
                    }
                });
            }

            log.Should().HaveCount(10);
        }

        [Test]
        public async Task When_the_sender_throws_then_other_events_continue_to_be_sent()
        {
            var log = new List<LogEntry>();
            var count = 0;

            using (new LogEntryDispatchQueue(send: async e =>
            {
                if (++count == 1)
                {
                    throw new Exception("oops");
                }
                log.Add(e);
            }))
            {
                WriteInParallel(10);
            }

            log.Should().HaveCount(9);
        }

        public void WriteInParallel(int degreesOfParallelism = 5, Action<int> write = null)
        {
            Parallel.ForEach(Enumerable.Range(1, degreesOfParallelism),
                             write ??
                             (i => Log.Write(() => i)));
        }
    }
}