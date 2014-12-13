// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using FluentAssertions;
using Its.Log.Instrumentation;
using Its.Recipes;
using NUnit.Framework;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class SensorAuthorizationTests
    {
        private string sensorName;

        [SetUp]
        public void SetUp()
        {
            sensorName = Any.AlphanumericString(10, 20);
        }

        [TearDown]
        public void TearDown()
        {
            DiagnosticSensor.Remove(sensorName);
            TestSensor.GetSensorValue = null;
        }

        [Test]
        public void Authorization_can_be_denied_for_all_sensors()
        {
            var configuration = new HttpConfiguration().MapSensorRoutes(ctx => false);
            var apiClient = new HttpClient(new HttpServer(configuration));
            DiagnosticSensor.Register(() => "hello", sensorName);

            apiClient.GetAsync("http://blammo.com/sensors/" + sensorName).Result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public void Authorization_can_be_denied_for_specific_sensors()
        {
            var configuration = new HttpConfiguration
            {
                IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always
            }.MapSensorRoutes(ctx =>
            {
                var name = (string) ctx.ControllerContext.RouteData.Values["name"];
                return !string.Equals(name, sensorName, StringComparison.OrdinalIgnoreCase);
            });
            var apiClient = new HttpClient(new HttpServer(configuration));
            DiagnosticSensor.Register(() => "hello", sensorName);

            apiClient.GetAsync("http://blammo.com/sensors/" + sensorName)
                     .Result
                     .ShouldFailWith(HttpStatusCode.Forbidden);
            apiClient.GetAsync("http://blammo.com/sensors/application")
                     .Result
                     .ShouldSucceed();
        }

        [Test]
        public void POST_to_sensors_returns_405()
        {
            var configuration = new HttpConfiguration().MapSensorRoutes(ctx => true);
            var apiClient = new HttpClient(new HttpServer(configuration));

            apiClient.PostAsync("http://blammo.com/sensors/", null).Result.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Test]
        public void A_message_handler_can_be_specified_to_perform_authentication_prior_to_the_sensor_authorization_check()
        {
            var authenticator = new Authenticator
            {
                Send = request =>
                {
                    var response = request.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("authenticated", "true");
                    return response;
                }
            };
            var httpConfig = new HttpConfiguration();

            HttpMessageHandler handler = HttpClientFactory.CreatePipeline(
                new HttpControllerDispatcher(httpConfig),
                new[] { authenticator });

            httpConfig.MapSensorRoutes(
                ctx => ctx.Response.Headers.Contains("authenticated") &&
                       ctx.Response.Headers.GetValues("authenticated").Single() == "true",
                handler: handler);

            var apiClient = new HttpClient(new HttpServer(httpConfig));

            apiClient.GetAsync("http://blammo.biz/sensors").Result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        public class Authenticator : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Send.IfNotNull()
                           .Then(send => Task.Run(() => send(request)))
                           .Else(() => base.SendAsync(request, cancellationToken));
            }

            public Func<HttpRequestMessage, HttpResponseMessage> Send;
        }
    }
}