// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using System.Threading.Tasks;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class DispatchQueueTests
    {
        [Test]
        public async Task Queue_can_dispatch_log_entries()
        {
            var log = new List<LogEntry>();
            using (new LogEntryDispatchQueue(send: async e => log.Add(e)))
            {
                using (Log.Enter(() => { }))
                {
                }
            }

            log.Should().HaveCount(2);
        }

        [Test]
        public async Task Queue_can_dispatch_telemetry_events()
        {
            var log = new List<Telemetry>();
            using (new TelemetryDispatchQueue(send: async e => log.Add(e)))
            {
                using (Log.With<Telemetry>().Enter(() => { }))
                {
                }
            }

            log.Should().HaveCount(1);
        }
    }
}