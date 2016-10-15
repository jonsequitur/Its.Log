// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using FluentAssertions;
using Its.Log.Instrumentation.Extensions;
using Its.Recipes;
using Newtonsoft.Json;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class MonitoringTestDiscoveryTests
    {
        private static HttpClient apiClient;

        public MonitoringTestDiscoveryTests()
        {
            apiClient = new TestApi(targets => targets.Add("staging", "widgetapi", new Uri("http://staging.widgets.com"))
                                                      .Add("production", "widgetapi", new Uri("http://widgets.com"))
                                                      .Add("staging", "sprocketapi", new Uri("http://staging.sprockets.com"))
                                                      .Add("production", "sprocketapi", new Uri("http://sprockets.com")));
        }

        [Test]
        public void API_exposes_void_methods()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi").Result;

            response.ShouldSucceed();

            var json = response.Content.ReadAsStringAsync().Result;

            json.Should().Contain("passing_void_test");
        }

        [Test]
        public void API_does_not_expose_private_methods()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi").Result;

            response.ShouldSucceed();

            var json = response.Content.ReadAsStringAsync().Result;

            json.Should().NotContain("not_a_test");
        }

        [Test]
        public void API_does_not_expose_static_methods()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi").Result;

            response.ShouldSucceed();

            var json = response.Content.ReadAsStringAsync().Result;

            json.Should().NotContain("not_a_test");
        }

        [Test]
        public void API_does_not_expose_environments_that_were_not_configured()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/" + Any.CamelCaseName()).Result;

            response.ShouldFailWith(HttpStatusCode.NotFound);
        }

        [Test]
        public void API_does_not_expose_applications_that_were_not_configured()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/production/" + Any.CamelCaseName()).Result;

            response.ShouldFailWith(HttpStatusCode.NotFound);
        }

        [Test]
        public void When_tests_are_queried_by_environment_then_tests_not_valid_for_that_enviroment_are_not_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/production/widgetapi").Result;

            response.ShouldSucceed();

            var json = response.JsonContent();

            JArray tests = json.Tests;

            tests.Should().NotContain(o => o.Value<string>("Url") == "internal_only_test");
        }

        [Test]
        public void When_tests_are_queried_by_application_then_tests_not_valid_for_that_application_are_not_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/production/sprocketapi/").Result;

            response.ShouldSucceed();

            var json = response.JsonContent();

            JArray tests = json.Tests;

            tests.Should().NotContain(o => o.Value<string>("Url").Contains("widgetapi_only_test"));
        }

        [Test]
        public void When_tests_are_queried_by_environment_then_routes_are_returned_with_the_environment_filled_in()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/production/widgetapi").Result;

            response.ShouldSucceed();

            var json = response.JsonContent();

            JArray tests = json.Tests;

            tests.Should().Contain(o => o.Value<string>("Url") == "http://blammo.com/tests/production/widgetapi/passing_test_returns_object");
        }

        [Test]
        public void When_two_tests_have_the_same_name_then_an_informative_error_URL_is_displayed()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/production/widgetapi").Result;

            response.ShouldSucceed();

            var json = response.JsonContent();

            JArray tests = json.Tests;

            tests.Should().Contain(o =>
                                   o.Value<string>("Url") == "http://blammo.com/tests/production/widgetapi/TEST_NAME_COLLISION_1");
        }

        [Test]
        public void When_two_tests_have_the_same_name_then_calling_the_error_test_returns_500_and_details()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/production/widgetapi/TEST_NAME_COLLISION_1").Result;

            var message = response.Content.ReadAsStringAsync().Result;

            response.ShouldFailWith(HttpStatusCode.InternalServerError);
            message.Should().Contain("Test could not be routed");
            message.Should().Contain("CollisionTest2.name_collision");
        }

        [Test]
        public void When_an_undefined_application_is_queried_then_an_informative_error_is_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/production/widgetapiz/is_reachable").Result;

            response.ShouldFailWith(HttpStatusCode.NotFound);
        }

        [Test]
        public void When_an_undefined_environment_is_queried_then_an_informative_error_is_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/productionz/widgetapi/is_reachable").Result;

            response.ShouldFailWith(HttpStatusCode.NotFound);
        }

        [Test]
        public void When_tests_are_queried_by_environment_but_not_application_then_tests_for_all_applications_are_shown()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/production").Result;

            response.ShouldSucceed();

            var json = response.JsonContent();

            JArray tests = json.Tests;

            tests.Should()
                 .Contain(o =>
                          o.Value<string>("Url") == "http://blammo.com/tests/production/widgetapi/is_reachable");
            tests.Should()
                 .Contain(o =>
                          o.Value<string>("Url") == "http://blammo.com/tests/production/sprocketapi/is_reachable");

            tests.Should()
                 .NotContain(o =>
                             o.Value<string>("Url") == "http://blammo.com/tests/staging/widgetapi/is_reachable");
            tests.Should()
                 .NotContain(o =>
                             o.Value<string>("Url") == "http://blammo.com/tests/staging/sprocketapi/is_reachable");
        }

        [Test]
        public void When_tests_are_queried_with_no_environment_or_application_then_all_tests_are_shown()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/").Result;

            response.ShouldSucceed();

            var json = response.JsonContent();

            JArray tests = json.Tests;

            tests.Should()
                 .Contain(o =>
                          o.Value<string>("Url") == "http://blammo.com/tests/production/widgetapi/is_reachable");
            tests.Should()
                 .Contain(o =>
                          o.Value<string>("Url") == "http://blammo.com/tests/production/sprocketapi/is_reachable");

            tests.Should()
                 .Contain(o =>
                          o.Value<string>("Url") == "http://blammo.com/tests/staging/widgetapi/is_reachable");
            tests.Should()
                 .Contain(o =>
                          o.Value<string>("Url") == "http://blammo.com/tests/staging/sprocketapi/is_reachable");
        }

        [Test]
        public void Specific_tests_can_be_routed_using_the_testTypes_argument()
        {
            var configuration = new HttpConfiguration();
            configuration.MapTestRoutes(configureTargets: targets =>
                                                          targets.Add("production", "widgetapi", new Uri("http://widgets.com")),
                                        testTypes: new[] {typeof (WidgetApiTests)});
            configuration.EnsureInitialized();

            var api = new HttpClient(new HttpServer(configuration));

            var response = api.GetAsync("http://blammo.com/tests/").Result;

            response.ShouldSucceed();

            var json = response.JsonContent();

            JArray tests = json.Tests;

            tests.Should()
                 .Contain(o =>
                          o.Value<string>("Url").Contains("widgetapi_only_test"));
            tests.Should()
                 .NotContain(o =>
                             o.Value<string>("Url").Contains("passing_test_returns_object"));
        }

        [Category("Tags")]
        [Test]
        public void when_tests_with_a_specific_tag_are_requested_then_tests_with_that_tag_are_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?apple=true").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("honeycrisp");
        }

        [Category("Tags")]
        [Test]
        public void when_tests_with_a_specific_tag_are_requested_then_untagged_tests_are_not_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?apple=true").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().NotContain("passing_test");
        }

        [Category("Tags")]
        [Test]
        public void when_tests_with_a_specific_tag_are_requested_then_tagged_tests_without_that_tag_are_not_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?apple=true").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().NotContain("tangerine");
        }

        [Category("Tags")]
        [Test]
        public void when_a_tag_is_not_specified_then_tests_with_tags_are_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("honeycrisp");
        }

        [Category("Tags")]
        [Test]
        public void when_a_tag_is_specified_that_multiple_tests_share_then_both_are_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?fruit=true").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("honeycrisp");
        }

        [Category("Tags")]
        [Test]
        public void when_tests_with_a_specific_tag_are_requested_to_be_excluded_then_they_are()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?apple=false").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().NotContain("honeycrisp");
        }

        [Category("Tags")]
        [Test]
        public void when_tests_with_a_specific_tag_are_requested_to_be_excluded_then_tagged_tests_without_that_tag_are_included()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?apple=false").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("tangerine");
        }

        [Category("Tags")]
        [Test]
        public void when_tests_with_a_specific_tag_are_requested_to_be_excluded_then_untagged_tests_are_included()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?apple=false").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("passing_test_returns_object");
        }

        [Category("Tags")]
        [Test]
        public async Task all_of_the_available_tags_are_exposed_by_the_API()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/");

            await response.ShouldSucceedAsync();

            JArray tests = response.Result.JsonContent().Tests;

            var honeycrisps = tests.Where(t => t.Value<string>("Url").EndsWith("honeycrisp")).ToArray();

            honeycrisps.Count().Should().BeGreaterThan(0);

            foreach (var honeycrisp in honeycrisps)
            {
                honeycrisp["Tags"].Should().Contain(v => v.Value<string>() == "apple");
                honeycrisp["Tags"].Should().Contain(v => v.Value<string>() == "fruit");
            }
        }

        [Category("Tags")]
        [Test]
        public void when_two_tags_are_required_then_only_tests_containing_both_are_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?apple=true&fruit=true").Result;

            response.ShouldSucceed();

            var json = response.Content.ReadAsStringAsync().Result;

            json.Should().Contain("honeycrisp");
            json.Should().NotContain("tangerine");
        }

        [Category("Tags")]
        [Test]
        public void when_a_tag_is_specific_the_casing_on_the_tag_does_not_matter()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/?Brooklyn=true").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("manhattan");
        }

        [Test]
        public void when_a_test_class_has_a_public_property_then_it_is_not_discovered_as_a_test()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().NotContain("SomeProperty");
        }

        [Test]
        public void when_a_public_void_method_has_optional_parameters_then_it_is_discovered_as_a_test()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("void_test_with_optional_parameters");
        }

        [Test]
        public void when_public_type_returning_methods_have_optional_parameters_then_they_are_discovered_as_a_tests()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("string_returning_test_with_optional_parameters");
        }

        [Test]
        public void when_a_public_method_has_non_optional_parameters_then_it_is_not_discovered_as_a_test()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().NotContain("test_with_non_optional_parameters");
        }

        [Test]
        public void when_a_test_with_optional_parameters_is_called_then_the_parameter_is_set_to_the_default_value()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/" +
                                              "string_returning_test_with_optional_parameters").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("bar");
        }

        [Test]
        public void when_a_test_with_optional_parameters_is_called_then_the_parameters_can_be_set_with_a_query_string_parameter()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/" +
                                              "string_returning_test_with_optional_parameters?foo=notbar").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("notbar");
        }

        [Test]
        public void when_passing_query_parameters_that_are_not_test_parameters_then_the_test_still_executes()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/" +
                                              "string_returning_test_with_optional_parameters?foo=notbar&apikey=iHaveNothingToDoWithYourTest").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("notbar");
            response.Content.ReadAsStringAsync().Result.Should().NotContain("iHaveNothingToDoWithYourTest");
        }

        [Test]
        public void when_a_parameter_of_the_wrong_type_is_supplied_then_a_useful_error_message_is_returned()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/" +
                                              "string_returning_test_with_optional_parameters?count=gronk").Result;

            response.ShouldFailWith(HttpStatusCode.BadRequest);
            response.Content.ReadAsStringAsync().Result.Should().Contain("The value specified for parameter 'count' could not be parsed as System.Int32");
        }

        [Test]
        public void when_the_first_optional_parameter_is_not_specified_then_the_test_still_works()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/" +
                                              "string_returning_test_with_optional_parameters?count=5").Result;

            response.ShouldSucceed();
            response.Content.ReadAsStringAsync().Result.Should().Contain("bar");
            response.Content.ReadAsStringAsync().Result.Should().Contain("5");
        }

        [Test]
        public void when_tests_with_parameters_are_called_repeatedly_with_different_parameters_then_the_different_parameters_are_used()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/" +
                                              "string_returning_test_with_optional_parameters?foo=1").Result;
            response.Content.ReadAsStringAsync().Result.Should().Contain("1");

            response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/" +
                                          "string_returning_test_with_optional_parameters").Result;
            response.Content.ReadAsStringAsync().Result.Should().Contain("bar");

            response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/" +
                                          "string_returning_test_with_optional_parameters?foo=final").Result;
            response.Content.ReadAsStringAsync().Result.Should().Contain("final");
        }

        [Test]
        public void when_a_test_accepts_input_parameters_then_the_input_parameter_names_are_discoverable()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result;

            response.ShouldSucceed();
            var content = JsonConvert.DeserializeObject<TestDiscoveryResponse>(response.Content.ReadAsStringAsync().Result);

            content.Tests.Single(t => t.Url == "http://blammo.com/tests/staging/widgetapi/string_returning_test_with_optional_parameters")
                   .Parameters.Should().ContainSingle(p => p.Name == "foo");
            content.Tests.Single(t => t.Url == "http://blammo.com/tests/staging/widgetapi/string_returning_test_with_optional_parameters")
                   .Parameters.Should().ContainSingle(p => p.Name == "count");
        }

        [Test]
        public void when_a_test_accepts_input_parameters_then_the_input_parameter_default_values_are_discoverable()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result;

            response.ShouldSucceed();
            var content = JsonConvert.DeserializeObject<TestDiscoveryResponse>(response.Content.ReadAsStringAsync().Result);

            content.Tests.Single(t => t.Url == "http://blammo.com/tests/staging/widgetapi/string_returning_test_with_optional_parameters")
                   .Parameters.Single(p => p.Name == "foo").DefaultValue.ShouldBeEquivalentTo("bar");
            content.Tests.Single(t => t.Url == "http://blammo.com/tests/staging/widgetapi/string_returning_test_with_optional_parameters")
                   .Parameters.Single(p => p.Name == "count").DefaultValue.ShouldBeEquivalentTo(1);
        }

        [Test]
        public void when_a_test_does_not_accept_input_parameters_then_queryParameters_is_null()
        {
            var response = apiClient.GetAsync("http://blammo.com/tests/staging/widgetapi/").Result;

            response.ShouldSucceed();
            var content = JsonConvert.DeserializeObject<TestDiscoveryResponse>(response.Content.ReadAsStringAsync().Result);

            content.Tests.Single(t => t.Url == "http://blammo.com/tests/staging/widgetapi/passing_test_returns_object")
                   .Parameters.Should().BeNull();

        }
    }

    public class GotTests : IMonitoringTest
    {
        public string SomeProperty { get; set; }

        public string passing_test_returns_object()
        {
            return "success!";
        }

        public bool passing_test_returns_struct()
        {
            return true;
        }

        public dynamic failing_test()
        {
            throw new Exception("oops!");
        }

        public void passing_void_test()
        {
        }

        public void void_test_with_optional_parameters(string foo = "bar")
        {
        }

        public void test_with_non_optional_parameters(string foo)
        {
        }

        public string string_returning_test_with_optional_parameters(string foo = "bar", int count = 1)
        {
            return $"{foo} - {count}";
        }

        public void failing_void_test()
        {
            throw new Exception("oops!");
        }

        public async Task failing_void_async_test()
        {
            throw new Exception("oops!");
        }

        public static dynamic not_a_test()
        {
            return null;
        }

        private dynamic also_not_a_test()
        {
            return null;
        }

        public void no_more_than_10_percent_of_calls_have_failed()
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

            telemetry.PercentageOf(t => !t.Succeeded).Should().BeLessThanOrEqualTo(10.Percent());
        }

        public void telemetry_without_failures()
        {
            Telemetry[] telemetry =
            {
                new Telemetry
                {
                    ElapsedMilliseconds = Any.Int(),
                    OperationName = Any.CamelCaseName(),
                    Succeeded = true,
                    UserIdentifier = Any.Email(),
                    Properties = {{"StatusCode", "200"}}
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

            telemetry.PercentageOf(t => t.Succeeded).Should().BeEqualTo(100.Percent());
        }
    }

    public class BigAppleTests : IHaveTags
    {
        public string[] Tags
        {
            get
            {
                return new[]
                {
                    "Brooklyn", "Queens"
                };
            }
        }

        public dynamic manhattan()
        {
            return "Empire State";
        }
    }

    public class AppleTests : IHaveTags
    {
        public string[] Tags
        {
            get
            {
                return new[]
                {
                    "apple", "fruit"
                };
            }
        }

        public dynamic honeycrisp()
        {
            return "Yum!";
        }
    }

    public class OrangeTests : IHaveTags
    {
        public string[] Tags
        {
            get
            {
                return new[]
                {
                    "orange", "fruit"
                };
            }
        }

        public dynamic tangerine()
        {
            return "Oooh!";
        }
    }

    public class InternalOnlyTests : IApplyToEnvironment
    {
        public dynamic internal_only_test()
        {
            return "success!";
        }

        public bool AppliesToEnvironment(string environment)
        {
            return !environment.Equals("production", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class WidgetApiTests : IApplyToApplication
    {
        public dynamic widgetapi_only_test()
        {
            return "success!";
        }

        public bool AppliesToApplication(string application)
        {
            return application.Equals("widgetapi", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class CollisionTest1 : IMonitoringTest
    {
        public dynamic name_collision()
        {
            return GetType().Name;
        }
    }

    public class CollisionTest2 : IMonitoringTest
    {
        public dynamic name_collision()
        {
            return GetType().Name;
        }
    }

    public class TestDiscoveryResponse
    {
        public Test[] Tests { get; set; }

        public class Test
        {
            public string Application { get; set; }
            public string Environment { get; set; }
            public string Url { get; set; }
            public Parameter[] Parameters { get; set; }

            public class Parameter
            {
                public string Name { get; set; }
                public object DefaultValue { get; set; }
            }
        }
    }
}