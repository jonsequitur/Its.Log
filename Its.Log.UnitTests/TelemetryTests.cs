// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class TelemetryTests
    {
        private CompositeDisposable disposables;
        private IList<Telemetry> telemetryEvents;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable();
            telemetryEvents = new List<Telemetry>();

            disposables.Add(Log.TelemetryEvents().Subscribe(e => { telemetryEvents.Add(e); }));
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public void Telemetry_events_are_emitted_on_exit_and_not_on_enter()
        {
            using (var activity = Log.With<Telemetry>().Enter(() => { }))
            {
                telemetryEvents.Should().BeEmpty();
                activity.MarkAsSuccessful();
            }

            telemetryEvents.Should().HaveCount(1);
        }

        [Test]
        public void Telemetry_events_indicate_success_if_marked_as_successful()
        {
            using (var activity = Log.With<Telemetry>().Enter(() => { }))
            {
                telemetryEvents.Should().BeEmpty();
                activity.MarkAsSuccessful();
            }

            telemetryEvents.Should().ContainSingle(e => e.Succeeded);
        }

        [Test]
        public async Task Telemetry_events_indicate_failure_if_not_confirmed()
        {
            using (Log.With<Telemetry>().Enter(() => { }))
            {
            }

            telemetryEvents.Should().ContainSingle(e => e.Succeeded == false);
        }

        [Test]
        public async Task Telemetry_events_contain_timing()
        {
            using (Log.With<Telemetry>().Enter(() => { }))
            {
                await Task.Delay(1000);
            }

            telemetryEvents.Single()
                           .ElapsedMilliseconds
                           .Should()
                           .BeInRange(1000, 1100);
        }

        [Test]
        public void Telemetry_events_contain_an_operation_name()
        {
            using (Log.With<Telemetry>().Enter(() => { }))
            {
            }

            telemetryEvents.Single()
                           .OperationName
                           .Should()
                           .Be("Telemetry_events_contain_an_operation_name");
        }

        [Test]
        public async Task The_operation_name_can_be_specified_programmatically()
        {
            using (Log.With<Telemetry>(t => t.OperationName = "hello!").Enter(() => { }))
            {
            }

            telemetryEvents.Single()
                           .OperationName
                           .Should()
                           .Be("hello!");
        }

        [Test]
        public async Task The_user_identifier_can_be_specified_programmatically()
        {
            using (Log.With<Telemetry>(t => t.UserIdentifier = "epic_user").Enter(() => { }))
            {
            }

            telemetryEvents.Single()
                           .UserIdentifier
                           .Should()
                           .Be("epic_user");
        }

    }
}
