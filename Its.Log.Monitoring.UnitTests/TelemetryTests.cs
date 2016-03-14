using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Its.Recipes;
using NUnit.Framework;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class TelemetryTests
    {
        private static HttpClient apiClient;

        public TelemetryTests()
        {
            apiClient = new TestApi(targets => targets.Add("staging", "widgetapi", new Uri("http://staging.widgets.com")));
        }

        [Test]
        public void a_request_to_a_telemetry_api_with_failed_telemetry_results_should_return_InternalServerError()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/telemetry_with_failures").Result;
            var json = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(json);
            response.ShouldFailWith(HttpStatusCode.InternalServerError);
        }

        [Test]
        public void a_request_to_a_telemetry_api_without_failed_telemetry_results_should_return_OK()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/telemetry_without_failures").Result;
            var json = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(json);
            response.ShouldFailWith(HttpStatusCode.OK);

        
        }

        [Test]
        public void testName()
        {
            Telemetry[] telemetry =
            {
                new Telemetry
                {
                    ElapsedMilliseconds = Any.Int(),
                    OperationName = Any.CamelCaseName(),
                    Succeeded = false,
                    UserIdentifier = Any.Email(),
                    Properties = {{"StatusCode", "500"}}
                },
                new Telemetry
                {
                    ElapsedMilliseconds = Any.Int(),
                    OperationName = Any.CamelCaseName(),
                    Succeeded = true,
                    UserIdentifier = Any.Email(),
                    Properties = {{"StatusCode", "200"}}
                }
            };

            var blah = new TelemetryTestResult(telemetry);

            Enumerable.Range(1, 5).Should().HaveCount(3);

            blah.ShouldNotContainAnyTelemetry(t => t.Succeeded == false);
            blah.ShouldHaveAtLeast(4, t => t.Succeeded == false);
            blah.ShouldHaveAtMost(4, t => t.Succeeded == false);
            blah.ShouldHaveBetween(4, 7, t => t.Succeeded == false);
        }


    }
}