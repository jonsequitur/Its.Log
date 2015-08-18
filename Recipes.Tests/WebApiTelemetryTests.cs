using System;
using System.Collections.Generic;
using FluentAssertions;
using Its.Log.Instrumentation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Recipes.Tests
{
    [TestFixture]
    public class WebApiTelemetryTests
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
        public async Task Telemetry_events_based_on_HTTP_responses_indicate_HTTP_status_code()
        {
            HttpResponseMessage response = null;

            using (var activity = Log.With<Telemetry>(t => t.For(response)).Enter(() => { }))
            {
                response = new HttpResponseMessage(HttpStatusCode.OK);
                activity.MarkAsSuccessful();
            }

            telemetryEvents.Single()
                           .HttpStatusCode
                           .Should()
                           .Be(HttpStatusCode.OK);
        }

        [Test]
        public async Task Telemetry_events_based_on_HTTP_responses_indicate_success_if_the_code_is_20x()
        {
            HttpResponseMessage response = null;

            using (var activity = Log.With<Telemetry>(t => t.For(response)).Enter(() => { }))
            {
                response = new HttpResponseMessage(HttpStatusCode.OK);
                activity.MarkAsSuccessful();
            }

            telemetryEvents.Single()
                           .Succeeded
                           .Should()
                           .BeTrue();
        }

        [Test]
        public async Task Telemetry_events_based_on_HTTP_responses_indicate_failure_if_the_code_is_40x()
        {
            HttpResponseMessage response = null;

            using (Log.With<Telemetry>(t => t.For(response)).Enter(() => { }))
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            telemetryEvents.Single()
                           .Succeeded
                           .Should()
                           .BeFalse();
        }

        [Test]
        public async Task Telemetry_events_based_on_HTTP_responses_indicate_failure_if_the_code_is_50x()
        {
            HttpResponseMessage response = null;

            using (Log.With<Telemetry>(t => t.For(response)).Enter(() => { }))
            {
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            telemetryEvents.Single()
                           .Succeeded
                           .Should()
                           .BeFalse();
        }

        [Test]
        public async Task Telemetry_events_based_on_HTTP_responses_contain_a_target_URI()
        {
            HttpResponseMessage response = null;

            using (Log.With<Telemetry>(t => t.For(response)).Enter(() => { }))
            {
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Get, @"http://contoso.com/")
                };
            }

            telemetryEvents.Single()
                           .RequestUri
                           .Should()
                           .Be(new Uri(@"http://contoso.com/"));
        }
    }
}