using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using FluentAssertions;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class MonitoringTestDependencyTests
    {
        [Test]
        public void Target_environment_is_available_by_declaring_a_dependency_on_Target_when_no_resolver_is_specified()
        {
            var api = new TestApi(configureTargets: targets => targets.Add("staging", "widgetapi", new Uri("http://localhost:81")));

            var response = api.GetAsync("http://blammo.com/tests/staging/widgetapi/get_target").Result;

            Console.WriteLine(api.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result.Content.ReadAsStringAsync().Result);

            response.ShouldSucceed(HttpStatusCode.OK);

            var result = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(result);

            string environment = JsonConvert.DeserializeObject<dynamic>(result)
                                            .Environment;

            environment.Should().Be("staging");
        }

        [Test]
        public async Task Dependencies_can_be_declared_that_are_specific_to_environment_and_application()
        {
            var api = new TestApi(configureTargets:
                                      targets => targets
                                                     .Add("production", "widgets", new Uri("http://widgets.com"),
                                                          t => t.Register<HttpClient>(() => new FakeHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK))
                                                          {
                                                              BaseAddress = new Uri("http://widgets.com")
                                                          }))
                                                     .Add("staging", "widgets", new Uri("http://staging.widgets.com"),
                                                          t => t.Register<HttpClient>(() => new FakeHttpClient(_ => new HttpResponseMessage(HttpStatusCode.GatewayTimeout))
                                                          {
                                                              BaseAddress = new Uri("http://staging.widgets.com")
                                                          })));

            // try production, which should be reachable
            var response = api.GetAsync("http://blammo.com/tests/production/widgets/is_reachable");
            await response.ShouldSucceed();

            // then staging, which should not be reachable
            response = api.GetAsync("http://blammo.com/tests/staging/widgets/is_reachable");
            await response.ShouldFailWith(HttpStatusCode.InternalServerError);
        }

        [Test]
        public void When_a_test_cannot_be_instantiated_due_to_missing_dependencies_then_the_URL_is_still_displayed()
        {
            var api = new TestApi(configureTargets: targets => targets.Add("production", "widgetapi", new Uri("http://localhost:81")));

            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi").Result;

            response.ShouldSucceed();

            var json = response.JsonContent();

            Console.WriteLine(json);

            JArray tests = json.Tests;

            tests.Should().Contain(o =>
                                   o.Value<string>("Url") == "http://blammo.com/tests/production/widgetapi/unsatisfiable_dependencies_test");
        }

        [Test]
        public void When_a_test_cannot_be_instantiated_due_to_missing_dependencies_then_calling_the_error_test_returns_500_with_details()
        {
            var api = new TestApi(configureTargets: targets => targets.Add("production", "widgetapi", new Uri("http://localhost:81")));

            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/unsatisfiable_dependencies_test").Result;

            var message = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(message);

            response.ShouldFailWith(HttpStatusCode.InternalServerError);

            message.Should()
                   .Contain(
                       "ArgumentException: Message = PocketContainer can't construct a System.Collections.Generic.IEnumerable`1[System.Collections.Generic.KeyValuePair`2[System.Nullable`1[System.DateTimeOffset],System.Collections.Generic.HashSet`1[System.Guid]]] unless you register it first. ☹");
        }

        [Test]
        public void Test_targets_require_absolute_URIs()
        {
            var configuration = new HttpConfiguration();

            Action map = () =>
                         configuration.MapTestRoutes(configureTargets: targets =>
                                                                       targets.Add("this", "that", new Uri("/relative/uri", UriKind.Relative)));

            map.ShouldThrow<ArgumentException>()
               .And
               .Message.Should().Contain("Base address must be an absolute URI");
        }

        [Test]
        public void HttpClient_is_configured_by_default_using_TestTarget_BaseAddress()
        {
            var api = new TestApi(configureTargets: targets => targets.Add("production", "widgetapi", new Uri("http://localhost:42")));

            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/HttpClient_BaseAddress").Result;

            var message = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(message);

            response.ShouldSucceed();

            message.Should().Contain("BaseAddress = http://localhost:42");
        }

        [Test]
        public void When_HttpClient_BaseAddress_is_set_in_dependency_registration_then_it_is_not_overridden()
        {
            var api = new TestApi(configureTargets: targets =>
                                                    targets
                                                        .Add("production", "widgetapi", new Uri("http://google.com"),
                                                             dependencies => dependencies.Register(() => new HttpClient
                                                             {
                                                                 BaseAddress = new Uri("http://bing.com")
                                                             })));

            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/HttpClient_BaseAddress").Result;

            var message = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(message);

            response.ShouldSucceed();

            message.Should().Contain("BaseAddress = http://bing.com");
        }
        
        [Test]
        public void When_HttpClient_BaseAddress_is_not_set_in_dependency_registration_then_it_is_set_to_the_test_target_configured_value()
        {
            var api = new TestApi(configureTargets: targets =>
                                                    targets
                                                        .Add("production", "widgetapi", new Uri("http://bing.com"),
                                                             dependencies => dependencies.Register(() => new HttpClient())));

            var response = api.GetAsync("http://blammo.com/tests/production/widgetapi/HttpClient_BaseAddress").Result;

            var message = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(message);

            response.ShouldSucceed();

            message.Should().Contain("BaseAddress = http://bing.com");
        }
    }

    public class TestsWithDependencies : IMonitoringTest
    {
        private readonly HttpClient httpClient;
        private readonly TestTarget testTarget;

        public TestsWithDependencies(HttpClient httpClient, TestTarget testTarget)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            if (testTarget == null)
            {
                throw new ArgumentNullException("testTarget");
            }
            this.httpClient = httpClient;
            this.testTarget = testTarget;
        }

        public async Task<dynamic> is_reachable()
        {
            return await httpClient.GetAsync("/sensors")
                                   .ShouldSucceed();
        }

        public string HttpClient_BaseAddress()
        {
            return "BaseAddress = " + httpClient.BaseAddress;
        }
    }

    public class TestsWithDependencyOnTarget : IMonitoringTest
    {
        private readonly TestTarget testTarget;

        public TestsWithDependencyOnTarget(TestTarget testTarget)
        {
            if (testTarget == null)
            {
                throw new ArgumentNullException("testTarget");
            }
            this.testTarget = testTarget;
        }

        public async Task<dynamic> get_target()
        {
            return testTarget;
        }
    }

    public class TestWithUnsatisfiableDependencies : IMonitoringTest
    {
        public TestWithUnsatisfiableDependencies(IEnumerable<KeyValuePair<DateTimeOffset?, HashSet<Guid>>> probablyNotRegistered)
        {
        }

        public void unsatisfiable_dependencies_test()
        {
        }
    }
}