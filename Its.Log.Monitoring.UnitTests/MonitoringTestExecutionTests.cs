// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Log.Instrumentation;
using NUnit.Framework;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class MonitoringTestExecutionTests
    {
        private static HttpClient api;

        public MonitoringTestExecutionTests()
        {
            Formatter<TraceBuffer>.Register(b => new
            {
                b.HasContent,
                HashCode = b.GetHashCode()
            }.ToLogString());

            api = new TestApi(targets => targets
                                             .Add("production", "widgetapi", new Uri("http://widgets.com"),
                                                  dependencies => dependencies.Register<HttpClient>(() => new FakeHttpClient(msg => new HttpResponseMessage(HttpStatusCode.OK))))
                                             .Add("staging", "widgetapi", new Uri("http://widgets.com"),
                                                  dependencies => dependencies.Register<HttpClient>(() => new FakeHttpClient(msg => new HttpResponseMessage(HttpStatusCode.OK))))
                                             .Add("production", "sprocketapi", new Uri("http://widgets.com"),
                                                  dependencies => dependencies.Register<HttpClient>(() => new FakeHttpClient(msg => new HttpResponseMessage(HttpStatusCode.OK)))));
        }

        [SetUp]
        public void SetUp()
        {
            TestsWithTraceOutput.GetResponse = () => "...and the response";
        }

        [TearDown]
        public void TearDown()
        {
            TestsWithTraceOutput.Barrier = null;
        }

        [Test]
        public void When_a_test_with_a_return_value_passes_then_a_200_is_returned()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/passing_test_returns_object").Result;

            response.ShouldSucceed(HttpStatusCode.OK);
        }

        [Test]
        public void When_a_test_with_a_void_return_value_passes_then_a_200_is_returned()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/passing_void_test").Result;

            response.ShouldSucceed(HttpStatusCode.OK);
        }

        [Test]
        public void When_a_test_passes_and_returns_an_object_then_the_response_contains_the_test_return_value()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/passing_test_returns_object").Result;

            var result = response.Content.ReadAsStringAsync().Result;

            result.Should().Contain("success!");
        }

        [Test]
        public void When_a_test_passes_and_returns_a_struct_then_the_response_contains_the_test_return_value()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/passing_test_returns_struct").Result;

            var result = response.Content.ReadAsStringAsync().Result;

            result.Should().Be("true");
        }

        [Test]
        public void When_a_test_with_a_return_value_throws_then_a_500_Test_Failed_is_returned()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/failing_test").Result;

            response.ShouldFailWith(HttpStatusCode.InternalServerError);
        }

        [Test]
        public void When_a_test_with_a_void_return_value_throws_then_a_500_Test_Failed_is_returned()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/failing_void_test").Result;

            response.ShouldFailWith(HttpStatusCode.InternalServerError);
        }

        [Test]
        public void When_a_test_with_a_return_value_fails_then_the_response_contains_the_test_return_value_and_exception_details()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/failing_test").Result;

            var result = response.Content.ReadAsStringAsync().Result;

            result.Should().Contain("oops!");
        }

        [Test]
        public void When_a_test_with_a_void_return_value_fails_then_the_response_contains_the_test_return_value_and_exception_details()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/failing_void_test").Result;

            var result = response.Content.ReadAsStringAsync().Result;

            result.Should().Contain("oops!");
        }

        [Test]
        public void When_a_test_is_not_valid_for_a_given_environment_then_calling_it_returns_404()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/internal_only_test").Result;

            response.ShouldFailWith(HttpStatusCode.NotFound);
        }

        [Test]
        public void When_a_test_is_not_valid_for_a_given_application_then_calling_it_returns_404()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/sprocketapi/widgetapi_only_test").Result;

            response.ShouldFailWith(HttpStatusCode.NotFound);
        }

        [Test]
        public void When_a_test_passes_then_the_response_contains_trace_output_written_by_the_test()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/write_to_trace").Result;

            var result = response.Content.ReadAsStringAsync().Result;

            result.Should().Contain("Application = widgetapi | Environment = production");
            result.Should().EndWith("...and the response\"");
        }

        [Test]
        public void When_a_test_passes_then_the_response_contains_its_log_output_written_by_the_test()
        {
            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/write_to_its_log").Result;

            var result = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(result);

            result.Should().Contain("Application = widgetapi | Environment = production");
            result.Should().EndWith("...and the response\"");
        }

        [Test]
        public async Task When_a_test_passes_then_the_response_does_not_contains_trace_output_written_by_other_tests()
        {
            TestsWithTraceOutput.Barrier = new Barrier(2);

            var productionResponse = api.GetAsync("http://blammo.com/tests/production/widgetapi/write_to_trace");
            var stagingResponse = api.GetAsync("http://blammo.com/tests/staging/widgetapi/write_to_trace");

            var productionResult = await productionResponse.Result.Content.ReadAsStringAsync();
            var stagingResult = await stagingResponse.Result.Content.ReadAsStringAsync();

            productionResult.Should().Contain("Environment = production");
            productionResult.Should().NotContain("Environment = staging");
            stagingResult.Should().Contain("Environment = staging");
            stagingResult.Should().NotContain("Environment = production");
        }

        [Test]
        public void When_a_test_fails_then_the_response_contains_trace_output_written_by_the_test()
        {
            TestsWithTraceOutput.GetResponse = () => { throw new Exception("Doh!"); };

            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/write_to_trace").Result;

            var result = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(result);

            result.Should().Contain("Application = widgetapi | Environment = production");
            result.Should().Contain("Doh!");
        }

        [Test]
        public async Task When_a_test_fails_then_the_response_contains_its_log_output_written_by_the_test()
        {
            TestsWithTraceOutput.GetResponse = () => { throw new Exception("Doh!"); };

            var response = await api.GetAsync("http://blammo.com/tests/production/widgetapi/write_to_trace");

            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine(result);

            result.Should().Contain("Application = widgetapi | Environment = production");
            result.Should().Contain("Doh!");
        }

        [Test]
        public async Task When_a_test_fails_then_the_response_does_not_contains_trace_output_written_by_other_tests()
        {
            TestsWithTraceOutput.Barrier = new Barrier(2);
            TestsWithTraceOutput.GetResponse = () => { throw new Exception("oh noes!"); };

            var productionResponse = api.GetAsync("http://blammo.com/tests/production/widgetapi/write_to_trace");
            var stagingResponse = api.GetAsync("http://blammo.com/tests/staging/widgetapi/write_to_trace");

            var productionResult = await productionResponse.Result.Content.ReadAsStringAsync();
            var stagingResult = await stagingResponse.Result.Content.ReadAsStringAsync();

            productionResult.Should().Contain("Environment = production");
            productionResult.Should().NotContain("Environment = staging");
            stagingResult.Should().Contain("Environment = staging");
            stagingResult.Should().NotContain("Environment = production");
            productionResult.Should().Contain("oh noes!");
            stagingResult.Should().Contain("oh noes!");
        }
    }

    public class TestsWithTraceOutput : IMonitoringTest
    {
        public static Barrier Barrier;

        public static Func<dynamic> GetResponse;

        private readonly TestTarget target;

        public TestsWithTraceOutput(TestTarget target)
        {
            this.target = target;
        }

        public async Task<dynamic> write_to_trace()
        {
            if (Barrier != null)
            {
                Console.WriteLine("before barrier: " + target);
                Console.WriteLine(new { TraceBuffer.Current }.ToLogString());

                await Task.Yield();
                Barrier.SignalAndWait(TimeSpan.FromSeconds(5));
                Console.WriteLine("after barrier: " + target);
                Console.WriteLine(new { TraceBuffer.Current }.ToLogString());
            }

            Trace.WriteLine(target.ToLogString());

            return GetResponse();
        }

        public dynamic write_to_its_log()
        {
            using (Instrumentation.Log.Enter(() => new { target }))
            {
            }
            return GetResponse();
        }
    }
}