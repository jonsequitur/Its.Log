// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Routing;
using FluentAssertions;
using NUnit.Framework;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class MonitoringTestHtmlTests
    {
        private static HttpClient apiClient;
        private static HttpConfiguration configuration;

        [Test]
        public void When_HTML_is_requested_then_the_tests_endpoint_returns_UI_bootstrap_HTML()
        {
            var response = ReqestTestsHtml();

            var result = response.Content.ReadAsStringAsync().Result;

            result.Should().StartWith(@"<!doctype html>");
        }

        [Test]
        public void When_HTML_is_requested_then_it_contains_a_semantically_versioned_script_link()
        {
            var response = ReqestTestsHtml();

            var result = response.Content.ReadAsStringAsync().Result;

            result.Should().Contain(@"<script src=""http://itsmonitoringux.azurewebsites.net/its.log.monitoring.js?monitoringVersion=");
        }

        [Test]
        public void The_script_location_for_the_UI_can_be_configured()
        {
            var response = ReqestTestsHtml("//itscdn.azurewebsites.net/monitoring/1.0.0/monitoring.js");

            var result = response.Content.ReadAsStringAsync().Result;

            result.Should().Contain(@"<script src=""//itscdn.azurewebsites.net/monitoring/1.0.0/monitoring.js?monitoringVersion=");
        }

        private static HttpResponseMessage ReqestTestsHtml(string testUiScript = null)
        {
            configuration = new HttpConfiguration();
            var constraintResolver = new DefaultInlineConstraintResolver();
            constraintResolver.ConstraintMap.Add("among", typeof(AmongConstraint));
            configuration.MapHttpAttributeRoutes(constraintResolver);
            configuration.MapTestRoutes(testUiScriptUrl: testUiScript);
            configuration.EnsureInitialized();

            var server = new HttpServer(configuration);
            apiClient = new HttpClient(server);
            
            var request = new HttpRequestMessage(HttpMethod.Get, "http://blammo.com/tests/");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            var response = apiClient.SendAsync(request).Result;
            return response;
        }
    }
}